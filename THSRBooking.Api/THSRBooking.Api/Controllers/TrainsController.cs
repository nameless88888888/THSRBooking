using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THSRBooking.Api.Data;
using THSRBooking.Api.Models;

namespace THSRBooking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TrainsController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 查詢指定起訖站、日期與時間區間的車次
        /// GET /api/trains/search?fromStationId=2&toStationId=7&travelDate=2025-11-20&timeFrom=06:00:00&timeTo=12:00:00
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<TrainSearchResultDto>>> Search(
            int fromStationId,
            int toStationId,
            DateTime travelDate,
            TimeSpan? timeFrom,
            TimeSpan? timeTo)
        {
            if (fromStationId == toStationId)
            {
                return BadRequest("起訖站不能相同");
            }

            // 1. 取出起訖站資訊（同時確認存在）
            var stations = await _db.Stations
                .Where(s => s.StationId == fromStationId || s.StationId == toStationId)
                .ToListAsync();

            var fromStation = stations.FirstOrDefault(s => s.StationId == fromStationId);
            var toStation = stations.FirstOrDefault(s => s.StationId == toStationId);

            if (fromStation == null || toStation == null)
            {
                return BadRequest("起訖站不存在");
            }

            // true = 北 → 南（南下），false = 南 → 北（北上）
            bool isSouthboundQuery = fromStation.OrderIndex < toStation.OrderIndex;

            // 2. 時間區間預設
            var tf = timeFrom ?? TimeSpan.Zero;
            var tt = timeTo ?? new TimeSpan(23, 59, 59);

            // 3. 找出同時停靠起訖站的車次
            //    ★ 依照查詢方向決定 StopOrder 的比較方式
            var query =
                from t in _db.Trains
                join fs in _db.TrainStops on t.TrainId equals fs.TrainId
                join ts in _db.TrainStops on t.TrainId equals ts.TrainId
                where fs.StationId == fromStationId
                   && ts.StationId == toStationId
                   && (isSouthboundQuery
                       ? fs.StopOrder < ts.StopOrder   // 南下：from 在 to 前面
                       : fs.StopOrder > ts.StopOrder)  // 北上：from 在 to 後面
                select new
                {
                    Train = t,
                    FromStop = fs,
                    ToStop = ts
                };

            var rawList = await query.ToListAsync();
            var travelDateOnly = travelDate.Date;

            // 4. 在記憶體裡計算出發 / 到達時間與行車時間
            var results = rawList
                .Select(x =>
                {
                    var departTimeSpan = x.FromStop.DepartureTime ?? x.FromStop.ArrivalTime ?? TimeSpan.Zero;
                    var arriveTimeSpan = x.ToStop.ArrivalTime ?? x.ToStop.DepartureTime ?? TimeSpan.Zero;

                    var departDateTime = travelDateOnly + departTimeSpan;
                    var arriveDateTime = travelDateOnly + arriveTimeSpan;

                    // 若到達時間 <= 出發時間，視為跨日，+1 天
                    if (arriveDateTime <= departDateTime)
                    {
                        arriveDateTime = arriveDateTime.AddDays(1);
                    }

                    var durationMinutes = (int)(arriveDateTime - departDateTime).TotalMinutes;

                    return new TrainSearchResultDto
                    {
                        TrainId = x.Train.TrainId,
                        TrainNo = x.Train.TrainNo,
                        DepartDateTime = departDateTime,
                        ArriveDateTime = arriveDateTime,
                        DurationMinutes = durationMinutes
                    };
                })
                // 過濾掉不合理的班次
                .Where(r => r.DurationMinutes > 0 && r.DurationMinutes <= 600)
                // 套用時間區間
                .Where(r =>
                    r.DepartDateTime.TimeOfDay >= tf &&
                    r.DepartDateTime.TimeOfDay <= tt)
                .OrderBy(r => r.DepartDateTime)
                .ToList();

            return Ok(results);
        }
    }
}
