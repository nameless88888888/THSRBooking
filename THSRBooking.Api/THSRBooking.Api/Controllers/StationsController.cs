using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THSRBooking.Api.Data;
using THSRBooking.Api.Models;

namespace THSRBooking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StationsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StationsController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /api/stations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StationDto>>> GetAll()
        {
            var stations = await _db.Stations
                .OrderBy(s => s.OrderIndex)
                .Select(s => new StationDto
                {
                    StationId = s.StationId,
                    Name = s.Name,
                    Code = s.Code,
                    OrderIndex = s.OrderIndex
                })
                .ToListAsync();

            return Ok(stations);
        }

        // GET: /api/stations/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<StationDto>> GetById(int id)
        {
            var s = await _db.Stations
                .Where(x => x.StationId == id)
                .Select(x => new StationDto
                {
                    StationId = x.StationId,
                    Name = x.Name,
                    Code = x.Code,
                    OrderIndex = x.OrderIndex
                })
                .FirstOrDefaultAsync();

            if (s == null)
                return NotFound();

            return Ok(s);
        }
    }
}
