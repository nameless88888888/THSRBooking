using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THSRBooking.Api.Data;
using THSRBooking.Api.Models;
using THSRBooking.Api.Models.Orders.THSRBooking.Api.Models;

namespace THSRBooking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // 訂單 API 一律要登入
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public OrdersController(AppDbContext db)
        {
            _db = db;
        }

        // --- 共用：從 JWT 取目前登入者的 UserId (uid claim) ---
        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("uid");
            if (claim == null)
            {
                throw new InvalidOperationException("找不到使用者身分（uid claim）。");
            }
            return int.Parse(claim.Value);
        }

        // --- 共用：根據 BaseFare + 票種計算實際金額 ---
        private decimal CalculateFare(decimal baseFare, string? passengerType)
        {
            var pt = (passengerType ?? "Adult").Trim();

            switch (pt)
            {
                // 全票
                case "Adult":
                case "全票":
                case "成人":
                    return baseFare;

                // 半價票（敬老 / 愛心 / 兒童）
                case "Senior":
                case "敬老票":
                case "Love":
                case "愛心票":
                case "Child":
                case "兒童票":
                case "孩童":
                    return Math.Round(baseFare * 0.5m, 0);

                // 免票兒童
                case "FreeChild":
                case "免票兒童":
                    return 0m;

                // 不認得就當全票
                default:
                    return baseFare;
            }
        }

        /// <summary>
        /// 建立訂單（需要登入）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderCreateRequest request)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(request.TravelDate) ||
                !DateTime.TryParse(request.TravelDate, out var travelDate))
            {
                return BadRequest("TravelDate 格式錯誤");
            }
            var travelDateOnly = travelDate.Date;

            var train = await _db.Trains.FindAsync(request.TrainId);
            if (train == null)
                return BadRequest("找不到指定車次");

            var fromStation = await _db.Stations.FindAsync(request.FromStationId);
            var toStation = await _db.Stations.FindAsync(request.ToStationId);
            if (fromStation == null || toStation == null)
                return BadRequest("起訖站錯誤");

            var passengers = request.Passengers ?? new List<OrderPassengerDto>();
            if (passengers.Count == 0)
                return BadRequest("請至少輸入一位乘客");

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. 先建立 Order（先不管 OrderNumber，讓 DB 生 OrderId）
                var order = new Order
                {
                    UserId = userId,
                    TrainId = request.TrainId,
                    FromStationId = request.FromStationId,
                    ToStationId = request.ToStationId,
                    TravelDate = travelDateOnly,
                    Status = "Confirmed",
                    CreatedAt = DateTime.Now
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();   // 這裡會拿到 order.OrderId

                // 2. 根據 OrderId 產生唯一的 OrderNumber
                order.OrderNumber = $"O{DateTime.Now:yyyyMMddHHmmssfff}{order.OrderId}";
                _db.Orders.Update(order);
                await _db.SaveChangesAsync();

                // 3. 正常化乘客資料
                var normalizedPassengers = passengers
                    .Select(p => new
                    {
                        PassengerName = p.PassengerName?.Trim() ?? "",
                        PassengerType = string.IsNullOrWhiteSpace(p.PassengerType) ? "Adult" : p.PassengerType,
                        CarNumber = p.CarNumber,
                        SeatRow = p.SeatRow,
                        SeatColumn = p.SeatColumn
                    })
                    .ToList();

                // 事先抓一次這段區間的票價
                var fare = await _db.TicketFares.FirstOrDefaultAsync(f =>
                    f.FromStationId == request.FromStationId &&
                    f.ToStationId == request.ToStationId);

                decimal baseFare = fare?.BaseFare ?? 0m;

                foreach (var p in normalizedPassengers)
                {
                    // 4-0 檢查座位是否已被訂走
                    var seatTaken = await _db.SeatReservations.AnyAsync(s =>
                        s.TrainId == request.TrainId &&
                        s.TravelDate == travelDateOnly &&
                        s.CarNumber == p.CarNumber &&
                        s.SeatRow == p.SeatRow &&
                        s.SeatColumn == p.SeatColumn &&
                        !s.IsCancelled);

                    if (seatTaken)
                    {
                        await tx.RollbackAsync();
                        return BadRequest($"座位 {p.CarNumber} 車 {p.SeatRow}{p.SeatColumn} 已被訂走，請重新選位。");
                    }

                    // 4-1 建 ticket：依據 BaseFare + 票種計算金額
                    decimal price = CalculateFare(baseFare, p.PassengerType);

                    var ticket = new OrderTicket
                    {
                        OrderId = order.OrderId,
                        PassengerName = p.PassengerName,
                        PassengerType = p.PassengerType,
                        CarNumber = p.CarNumber,
                        SeatRow = p.SeatRow,
                        SeatColumn = p.SeatColumn,
                        Price = price
                    };

                    _db.OrderTickets.Add(ticket);
                    await _db.SaveChangesAsync();  // 取得 OrderTicketId

                    // 4-2 建 seat reservation
                    var seatRes = new SeatReservation
                    {
                        TrainId = request.TrainId,
                        TravelDate = travelDateOnly,
                        FromStationId = request.FromStationId,
                        ToStationId = request.ToStationId,
                        CarNumber = p.CarNumber,
                        SeatRow = p.SeatRow,
                        SeatColumn = p.SeatColumn,
                        OrderId = order.OrderId,
                        OrderTicketId = ticket.OrderTicketId,
                        IsCancelled = false
                    };

                    _db.SeatReservations.Add(seatRes);
                    await _db.SaveChangesAsync();
                }

                // 5. 計算總金額並存回 Order
                var totalAmount = await _db.OrderTickets
                    .Where(ot => ot.OrderId == order.OrderId)
                    .SumAsync(ot => ot.Price);

                order.TotalAmount = totalAmount;
                _db.Orders.Update(order);
                await _db.SaveChangesAsync();

                // 6. 回傳 tickets 明細
                var ticketList = await _db.OrderTickets
                    .Where(ot => ot.OrderId == order.OrderId)
                    .Select(ot => new
                    {
                        ot.PassengerName,
                        ot.PassengerType,
                        ot.CarNumber,
                        ot.SeatRow,
                        ot.SeatColumn,
                        ot.Price
                    })
                    .ToListAsync();

                await tx.CommitAsync();

                return Ok(new
                {
                    orderId = order.OrderId,
                    orderNumber = order.OrderNumber,
                    message = "訂票成功",
                    trainNo = train.TrainNo,
                    travelDate = travelDateOnly,
                    fromStation = fromStation.Name,
                    toStation = toStation.Name,
                    passengerCount = normalizedPassengers.Count,
                    totalAmount = totalAmount,
                    tickets = ticketList
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"建立訂單時發生錯誤：{ex.Message}");
            }
        }

        /// <summary>
        /// 修改訂單（目前只允許改乘客姓名、票種，座位與車次不變）
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] OrderUpdateRequest request)
        {
            var userId = GetCurrentUserId();

            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound("找不到此訂單或無權限修改。");
            }

            if (order.Status == "Cancelled")
            {
                return BadRequest("已取消的訂單無法修改。");
            }

            if (request == null || request.Tickets == null || request.Tickets.Count == 0)
            {
                return BadRequest("沒有任何要更新的乘客資料。");
            }

            // 先抓這張訂單所有 ticket
            var tickets = await _db.OrderTickets
                .Where(ot => ot.OrderId == order.OrderId)
                .ToListAsync();

            if (!tickets.Any())
            {
                return BadRequest("此訂單沒有任何票券資料，無法修改。");
            }

            // 查這段區間的 BaseFare
            var fare = await _db.TicketFares
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.FromStationId == order.FromStationId &&
                    f.ToStationId == order.ToStationId);

            decimal baseFare = fare?.BaseFare ?? 0m;

            var ticketDict = tickets.ToDictionary(t => t.OrderTicketId);

            foreach (var t in request.Tickets)
            {
                if (!ticketDict.TryGetValue(t.OrderTicketId, out var entity))
                {
                    // 找不到就略過（也可直接 return BadRequest）
                    continue;
                }

                entity.PassengerName = t.PassengerName?.Trim() ?? "";
                entity.PassengerType = string.IsNullOrWhiteSpace(t.PassengerType)
                    ? "Adult"
                    : t.PassengerType;

                entity.Price = CalculateFare(baseFare, entity.PassengerType);
            }

            // 重算訂單總金額
            order.TotalAmount = tickets.Sum(t => t.Price);
            _db.Orders.Update(order);
            await _db.SaveChangesAsync();

            // 組回傳 DTO（與 GetOrder 一致）
            var trainNo = await _db.Trains
                .Where(t => t.TrainId == order.TrainId)
                .Select(t => t.TrainNo)
                .FirstOrDefaultAsync() ?? "";

            var fromStationName = await _db.Stations
                .Where(s => s.StationId == order.FromStationId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync() ?? "";

            var toStationName = await _db.Stations
                .Where(s => s.StationId == order.ToStationId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync() ?? "";

            var ticketDtos = tickets
                .Select(ot => new OrderTicketItemDto
                {
                    OrderTicketId = ot.OrderTicketId,
                    PassengerName = ot.PassengerName,
                    PassengerType = ot.PassengerType,
                    CarNumber = ot.CarNumber,
                    SeatRow = ot.SeatRow,
                    SeatColumn = ot.SeatColumn,
                    Price = ot.Price
                })
                .ToList();

            var dto = new OrderDetailDto
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber ?? "",
                TrainNo = trainNo,
                TravelDate = order.TravelDate,
                FromStationName = fromStationName,
                ToStationName = toStationName,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                TotalAmount = order.TotalAmount,
                Tickets = ticketDtos
            };

            return Ok(dto);
        }

        /// <summary>
        /// 查詢某車次 / 日期 / 車廂 已被訂的座位
        /// </summary>
        [HttpGet("seatStatus")]
        public async Task<IActionResult> GetSeatStatus(
            [FromQuery] int trainId,
            [FromQuery] DateTime travelDate,
            [FromQuery] int carNumber)
        {
            var dateOnly = travelDate.Date;

            var seats = await _db.SeatReservations
                .Where(s => s.TrainId == trainId
                            && s.TravelDate == dateOnly
                            && s.CarNumber == carNumber
                            && !s.IsCancelled)
                .Select(s => new
                {
                    s.SeatRow,
                    s.SeatColumn
                })
                .ToListAsync();

            return Ok(seats);
        }

        /// <summary>
        /// 取消訂單（僅限訂單本人）
        /// </summary>
        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = GetCurrentUserId();

            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound("找不到此訂單或無權限取消。");
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1) 先刪 SeatReservations（避免 FK 卡住）
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM SeatReservations WHERE OrderId = {order.OrderId}");

                // 2) 再刪 OrderTickets
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM OrderTickets WHERE OrderId = {order.OrderId}");

                // 3) 最後刪 Orders
                _db.Orders.Remove(order);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();

                return Ok(new
                {
                    orderId = order.OrderId,
                    message = "訂單已刪除"
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"取消 / 刪除訂單時發生錯誤：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢某車次在指定日期，已被訂走的座位（不分車廂）
        /// </summary>
        [HttpGet("seats")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<SeatStatusDto>>> GetSeatStatus(
            [FromQuery] int trainId,
            [FromQuery] string travelDate,
            [FromQuery] int fromStationId,
            [FromQuery] int toStationId
        )
        {
            if (!DateTime.TryParse(travelDate, out var dt))
            {
                return BadRequest("travelDate 格式錯誤");
            }
            var dateOnly = dt.Date;

            var reserved = await _db.SeatReservations
                .Where(s => s.TrainId == trainId
                            && s.TravelDate == dateOnly
                            && !s.IsCancelled)
                .Select(s => new SeatStatusDto
                {
                    CarNumber = s.CarNumber,
                    SeatRow = s.SeatRow,
                    SeatColumn = s.SeatColumn
                })
                .ToListAsync();

            return Ok(reserved);
        }

        /// <summary>
        /// 查詢目前登入者的訂單清單
        /// </summary>
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<OrderSummaryDto>>> GetMyOrders()
        {
            var userId = GetCurrentUserId();

            var baseList = await
                (from o in _db.Orders
                 join t in _db.Trains on o.TrainId equals t.TrainId
                 join fs in _db.Stations on o.FromStationId equals fs.StationId
                 join ts in _db.Stations on o.ToStationId equals ts.StationId
                 where o.UserId == userId
                 select new
                 {
                     o.OrderId,
                     t.TrainNo,
                     o.TravelDate,
                     FromStationName = fs.Name,
                     ToStationName = ts.Name,
                     o.Status,
                     o.CreatedAt,
                     o.TotalAmount
                 })
                .OrderByDescending(x => x.TravelDate)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync();

            var orderIds = baseList.Select(x => x.OrderId).ToList();

            var tickets = await _db.OrderTickets
                .Where(ot => orderIds.Contains(ot.OrderId))
                .Select(ot => new
                {
                    ot.OrderId,
                    ot.PassengerName,
                    ot.CarNumber,
                    ot.SeatRow,
                    ot.SeatColumn
                })
                .ToListAsync();

            var seatsDict = tickets
                .GroupBy(t => t.OrderId)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join("、",
                            g.Select(t =>
                                $"{t.PassengerName}({t.CarNumber}車{t.SeatRow}{t.SeatColumn})"
                            ))
                );

            var result = baseList
                .Select(x => new OrderSummaryDto
                {
                    OrderId = (int)x.OrderId,
                    TrainNo = x.TrainNo,
                    TravelDate = x.TravelDate,
                    FromStationName = x.FromStationName,
                    ToStationName = x.ToStationName,
                    PassengerCount = tickets.Count(t => t.OrderId == x.OrderId),
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    PassengerSeats = seatsDict.TryGetValue(x.OrderId, out var s) ? s : "",
                    TotalAmount = x.TotalAmount
                })
                .ToList();

            return Ok(result);
        }

        /// <summary>
        /// 取得單一訂單明細（目前登入者）
        /// GET /api/orders/{id}
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderDetailDto>> GetOrder(int id)
        {
            var userId = GetCurrentUserId();

            var orderBase = await
                (from o in _db.Orders
                 join t in _db.Trains on o.TrainId equals t.TrainId
                 join fs in _db.Stations on o.FromStationId equals fs.StationId
                 join ts in _db.Stations on o.ToStationId equals ts.StationId
                 where o.OrderId == id && o.UserId == userId
                 select new
                 {
                     o.OrderId,
                     o.OrderNumber,
                     t.TrainNo,
                     o.TravelDate,
                     FromStationName = fs.Name,
                     ToStationName = ts.Name,
                     o.Status,
                     o.CreatedAt,
                     o.TotalAmount
                 })
                .FirstOrDefaultAsync();

            if (orderBase == null)
            {
                return NotFound("找不到此訂單或無權限查看。");
            }

            var ticketList = await _db.OrderTickets
                .Where(ot => ot.OrderId == orderBase.OrderId)
                .Select(ot => new OrderTicketItemDto
                {
                    OrderTicketId = ot.OrderTicketId,
                    PassengerName = ot.PassengerName,
                    PassengerType = ot.PassengerType,
                    CarNumber = ot.CarNumber,
                    SeatRow = ot.SeatRow,
                    SeatColumn = ot.SeatColumn,
                    Price = ot.Price
                })
                .ToListAsync();

            var dto = new OrderDetailDto
            {
                OrderId = orderBase.OrderId,
                OrderNumber = orderBase.OrderNumber ?? "",
                TrainNo = orderBase.TrainNo,
                TravelDate = orderBase.TravelDate,
                FromStationName = orderBase.FromStationName,
                ToStationName = orderBase.ToStationName,
                Status = orderBase.Status,
                CreatedAt = orderBase.CreatedAt,
                TotalAmount = orderBase.TotalAmount,
                Tickets = ticketList
            };

            return Ok(dto);
        }
    }

    // ======= DTOs for Order Detail & Update =======

    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = "";
        public string TrainNo { get; set; } = "";
        public DateTime TravelDate { get; set; }
        public string FromStationName { get; set; } = "";
        public string ToStationName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderTicketItemDto> Tickets { get; set; } = new();
    }

    public class OrderTicketItemDto
    {
        public int OrderTicketId { get; set; }
        public string PassengerName { get; set; } = "";
        public string PassengerType { get; set; } = "";
        public int CarNumber { get; set; }
        public int SeatRow { get; set; }
        public string SeatColumn { get; set; } = "";
        public decimal Price { get; set; }
    }

    public class OrderUpdateRequest
    {
        public List<OrderUpdateTicketDto> Tickets { get; set; } = new();
    }

    public class OrderUpdateTicketDto
    {
        public int OrderTicketId { get; set; }
        public string PassengerName { get; set; } = "";
        public string PassengerType { get; set; } = "";  // Adult / Child / Senior / Love / FreeChild 或中文別名
    }
}
