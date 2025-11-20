namespace THSRBooking.Api.Models
{
    public class Station
    {
        public int StationId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int OrderIndex { get; set; }
    }
}
