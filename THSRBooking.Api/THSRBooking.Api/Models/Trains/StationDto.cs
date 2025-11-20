// Models/Trains/StationDto.cs
namespace THSRBooking.Api.Models
{
    public class StationDto
    {
        public int StationId { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public int OrderIndex { get; set; }
    }
}
