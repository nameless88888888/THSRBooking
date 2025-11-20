using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THSR.Api.Data;
using THSR.Api.Models;

namespace THSR.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // => api/Orders
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public OrdersController(AppDbContext db)
        {
            _db = db;
        }

        // ===========================
        //  POST /api/orders
        //  建立訂單 + 票券（訂票）
        // ===========================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
        {
            if (req == null)
                return BadRequest("Request 不可為空。");

            if (req.UserId <= 0 || req.TrainId <= 0 || string.IsNullOrWhiteSpace(req.PassengerName))
            {
                return BadRequest("UserId、TrainId 與乘客姓名為必填。");
            }

            // 1. 確認車次存在
            var train = await _db.Trains
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TrainID == req.TrainId);

            if (train == null)
            {
                return BadRequest($"找不到指定的車次 (TrainID={req.TrainId})。");
            }

            // TODO：這裡之後可以檢查座位是否已被訂走（目前先略過）
            // === 檢查剩餘座位數 ===
            var soldCount = await _db.Tickets
                .Where(t => t.TrainID == req.TrainId && t.Status == "Valid")
                .CountAsync();

            if (soldCount >= train.TotalSeats)
            {
                return BadRequest("此班車座位已售完，無法再訂票。");
            }
            // === 檢查指定座位是否已被訂走 ===
            if (!string.IsNullOrWhiteSpace(req.SeatNumber))
            {
                var seatUsed = await _db.Tickets
                    .AnyAsync(t =>
                        t.TrainID == req.TrainId &&
                        t.Status == "Valid" &&
                        t.SeatNumber == req.SeatNumber);

                if (seatUsed)
                {
                    return BadRequest($"座位 {req.SeatNumber} 已被訂走，請換座位。");
                }
            }
            // 2. 建 Passenger（簡化做法：每次訂票建立一筆乘客資料）
            var passenger = new Passenger
            {
                UserID = req.UserId,
                Name = req.PassengerName,
                PhoneNumber = req.PassengerPhone,
                IDNumber = req.PassengerIdNumber
            };

            _db.Passenger.Add(passenger);
            await _db.SaveChangesAsync(); // 存完才能拿到 PassengerID

            // 3. 計算票價（目前先用固定金額，你也可以之後改成依照區間計算）
            var price = 1000;

            // 4. 建 Order 主檔
            var order = new Order
            {
                UserID = req.UserId,
                OrderTime = DateTime.Now,
                TotalAmount = price,
                Status = "Booked"
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(); // 存完才能拿到 OrderID

            // 5. 建 Ticket
            var ticket = new Ticket
            {
                OrderID = order.OrderID,
                PassengerID = passenger.PassengerID,
                TrainID = req.TrainId,
                SeatNumber = req.SeatNumber,
                Price = price,
                TicketType = "Normal",
                Status = "Valid"
            };

            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync();

            // 6. 回傳結果
            return Ok(new
            {
                orderId = order.OrderID,
                ticketId = ticket.TicketID,
                trainNo = train.TrainNo,
                passengerName = passenger.Name,
                seatNumber = ticket.SeatNumber,
                price = ticket.Price
            });
        }

        // =================================
        //  GET /api/orders?userId=1
        //  某會員的訂單列表
        // =================================
        [HttpGet]
        public async Task<IActionResult> GetByUser([FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("userId 必須大於 0。");

            // 先抓這個 user 的所有訂單
            var orders = await _db.Orders
                .Where(o => o.UserID == userId)
                .OrderByDescending(o => o.OrderTime)
                .ToListAsync();

            var result = new List<OrderListItem>();

            foreach (var o in orders)
            {
                // 找這張訂單的一張票（任意一張即可）
                var ticket = await _db.Tickets
                    .Where(t => t.OrderID == o.OrderID)
                    .FirstOrDefaultAsync();

                if (ticket == null)
                    continue; // 沒票就略過

                var train = await _db.Trains
                    .AsNoTracking()
                    .FirstOrDefaultAsync(tr => tr.TrainID == ticket.TrainID);

                if (train == null)
                    continue;

                var dep = await _db.Stations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.StationID == train.DepartureStationID);
                var arr = await _db.Stations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.StationID == train.ArrivalStationID);

                if (dep == null || arr == null)
                    continue;

                result.Add(new OrderListItem
                {
                    OrderId = o.OrderID,
                    OrderTime = o.OrderTime,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    TrainNo = train.TrainNo,
                    DepartureStation = dep.StationName,
                    ArrivalStation = arr.StationName,
                    DepartureTime = train.DepartureTime
                });
            }

            return Ok(result); // 就算沒有訂單，也會回傳 []
        }

        // ======================================
        //  GET /api/orders/{orderId}
        //  單一訂單明細（含票券清單）
        // ======================================
        [HttpGet("{orderId:int}")]
        public async Task<IActionResult> GetDetail(int orderId)
        {
            // 1. 找訂單主檔
            var order = await _db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
                return NotFound($"找不到訂單 (OrderID={orderId})。");

            // 2. 找這張訂單的票券
            var tickets = await _db.Tickets
                .Where(t => t.OrderID == orderId)
                .ToListAsync();

            if (!tickets.Any())
                return BadRequest($"此訂單沒有任何票券資料 (OrderID={orderId})。");

            // 假設所有票都是同一班車，拿第一張的 TrainID
            var firstTicket = tickets.First();
            var train = await _db.Trains
                .AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.TrainID == firstTicket.TrainID);

            if (train == null)
                return BadRequest($"找不到對應車次 (TrainID={firstTicket.TrainID})。");

            var dep = await _db.Stations
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StationID == train.DepartureStationID);
            if (dep == null)
                return BadRequest($"找不到出發站 (StationID={train.DepartureStationID})。");

            var arr = await _db.Stations
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StationID == train.ArrivalStationID);
            if (arr == null)
                return BadRequest($"找不到到達站 (StationID={train.ArrivalStationID})。");

            // 3. 組訂單主檔 DTO
            var dto = new OrderDetailDto
            {
                OrderId = order.OrderID,
                OrderTime = order.OrderTime,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                TrainNo = train.TrainNo,
                DepartureStation = dep.StationName,
                ArrivalStation = arr.StationName,
                DepartureTime = train.DepartureTime,
                ArrivalTime = train.ArrivalTime,
                Tickets = new List<OrderTicketItem>()
            };

            // 4. 組票券清單
            foreach (var tk in tickets)
            {
                var passenger = await _db.Passenger
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PassengerID == tk.PassengerID);

                dto.Tickets.Add(new OrderTicketItem
                {
                    TicketId = tk.TicketID,
                    PassengerName = passenger?.Name ?? "(未知乘客)",
                    SeatNumber = tk.SeatNumber,
                    Price = tk.Price,
                    TicketType = tk.TicketType,
                    Status = tk.Status
                });
            }

            return Ok(dto);
        }

        // ======================================
        //  POST /api/orders/{orderId}/cancel
        //  取消訂單（模擬，不退錢）
        // ======================================
        [HttpPost("{orderId:int}/cancel")]
        public async Task<IActionResult> Cancel(int orderId)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderID == orderId);
            if (order == null)
                return NotFound($"找不到訂單 (OrderID={orderId})。");

            if (order.Status == "Canceled")
                return BadRequest("此訂單已經取消。");

            order.Status = "Canceled";

            var tickets = await _db.Tickets
                .Where(t => t.OrderID == orderId)
                .ToListAsync();

            foreach (var t in tickets)
            {
                t.Status = "Canceled";
            }

            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
