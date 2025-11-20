using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THSRBooking.Api.Data;

namespace THSRBooking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeatsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SeatsController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 取得某車廂的座位配置（只回傳存在的座位）
        /// GET /api/seats/layout?carNumber=6
        /// </summary>
        [HttpGet("layout")]
        public async Task<ActionResult<IEnumerable<object>>> GetLayout([FromQuery] int carNumber)
        {
            if (carNumber <= 0)
            {
                return BadRequest("carNumber 必須大於 0");
            }

            var layout = await _db.SeatLayouts
                .Where(s => s.CarNumber == carNumber && s.IsEnabled)
                .OrderBy(s => s.SeatRow)
                .ThenBy(s => s.SeatColumn)
                .Select(s => new
                {
                    seatRow = s.SeatRow,
                    seatColumn = s.SeatColumn,
                    isWindow = s.IsWindow,
                    isAisle = s.IsAisle
                })
                .ToListAsync();

            if (!layout.Any())
            {
                return NotFound("找不到該車廂的座位配置");
            }

            return Ok(layout);
        }
    }
}
