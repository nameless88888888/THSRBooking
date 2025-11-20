namespace THSRBooking.Api.Models
{
    public class SeatStatusDto
    {
        public int CarNumber { get; set; }
        public int SeatRow { get; set; }
        public string SeatColumn { get; set; } = string.Empty;
    }
}
