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
    [Route("api/[controller]")] // => api/trains
    public class TrainsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TrainsController(AppDbContext db)
        {
            _db = db;
        }

        // GET /api/trains/search?departureStationId=1&arrivalStationId=4&date=2025-11-17
        // GET /api/trains/search?departureStationId=1&arrivalStationId=4&date=2025-11-17
        // GET /api/trains/search?departureStationId=2&arrivalStationId=7&date=2025-11-20&time=09:00
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] int departureStationId,
            [FromQuery] int arrivalStationId,
            [FromQuery] DateTime date,
            [FromQuery] string? time = null      // 可選的時間參數，格式 HH:mm
        )
        {
            if (departureStationId <= 0 || arrivalStationId <= 0)
                return BadRequest("起訖站必須大於 0。");

            if (departureStationId == arrivalStationId)
                return BadRequest("起訖站不能相同。");

            // 先確認車站存在
            var depStation = await _db.Stations
                .FirstOrDefaultAsync(s => s.StationID == departureStationId);
            var arrStation = await _db.Stations
                .FirstOrDefaultAsync(s => s.StationID == arrivalStationId);

            if (depStation == null || arrStation == null)
                return BadRequest("找不到對應的起訖站。");

            // 當天 00:00 ~ 隔天 00:00
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            DateTime? rangeStart = null;
            DateTime? rangeEnd = null;

            // 如果有帶 time，就縮成 time ~ time+2 小時
            if (!string.IsNullOrWhiteSpace(time))
            {
                if (!TimeSpan.TryParse(time, out var ts))
                {
                    return BadRequest("時間格式錯誤，請使用 HH:mm，例如 09:00。");
                }

                rangeStart = dayStart.Add(ts);
                rangeEnd = rangeStart.Value.AddHours(2);
            }

            // ⚠️重點：用 TrainStops 來查這一天所有「有經過起訖站」的班次
            var trainListQuery =
                from t in _db.Trains
                join depStop in _db.TrainStops
                    on t.TrainID equals depStop.TrainID
                join arrStop in _db.TrainStops
                    on t.TrainID equals arrStop.TrainID
                where depStop.StationID == departureStationId
                   && arrStop.StationID == arrivalStationId
                   && depStop.StopOrder < arrStop.StopOrder                 // 先經過起站再經過訖站
                   && depStop.DepartureTime >= dayStart
                   && depStop.DepartureTime < dayEnd

                select new
                {
                    t.TrainID,
                    t.TrainNo,
                    DepTime = depStop.DepartureTime,
                    ArrTime = arrStop.ArrivalTime,
                    t.TotalSeats
                };

            // 如果有選時間，套上兩小時內的篩選
            if (rangeStart.HasValue)
            {
                trainListQuery = trainListQuery
                    .Where(x => x.DepTime >= rangeStart.Value &&
                                x.DepTime < rangeEnd.Value);
            }

            var trainList = await trainListQuery
                .OrderBy(x => x.DepTime)
                .ToListAsync();

            var result = new List<object>();

            foreach (var item in trainList)
            {
                // 已售座位數（依你的 Tickets 欄位調整）
                int soldCount = await _db.Tickets
                    .Where(k => k.TrainID == item.TrainID && k.Status == "Valid")
                    .CountAsync();

                int remainingSeats = item.TotalSeats - soldCount;
                if (remainingSeats < 0) remainingSeats = 0;

                result.Add(new
                {
                    trainID = item.TrainID,
                    trainNo = item.TrainNo,
                    departureStation = depStation.StationName,
                    arrivalStation = arrStation.StationName,
                    departureTime = item.DepTime,
                    arrivalTime = item.ArrTime,
                    totalSeats = item.TotalSeats,
                    remainingSeats = remainingSeats
                });
            }

            return Ok(result);
        }

    }
}
