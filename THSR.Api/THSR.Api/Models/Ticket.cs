namespace THSR.Api.Models
{
    public class Ticket
    {
        public int TicketID { get; set; }
        public int OrderID { get; set; }
        public int PassengerID { get; set; }
        public int TrainID { get; set; }
        public string? SeatNumber { get; set; }
        public int Price { get; set; }
        public string TicketType { get; set; } = null!; // Normal / Student...
        public string Status { get; set; } = null!;     // Valid / Canceled
    }
}
