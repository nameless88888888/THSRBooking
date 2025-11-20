using Microsoft.AspNetCore.Mvc;
using THSR.Api.Data;

namespace THSR.Api.Controllers
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

        // GET /api/stations
        [HttpGet]
        public IActionResult GetAll()
        {
            var stations = _db.Stations
                .OrderBy(s => s.StationID)
                .ToList();

            return Ok(stations);
        }
    }
}
