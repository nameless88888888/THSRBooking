namespace THSRBooking.Api.Models
{
    public class OrderTicket
    {
        public int OrderTicketId { get; set; }    // PK 還是 int，因為 DB 目前是 INT IDENTITY
        public int OrderId { get; set; }         // ← 這裡改成 long

        public string PassengerName { get; set; } = string.Empty;
        public string PassengerType { get; set; } = "Adult";
        public int CarNumber { get; set; }
        public int SeatRow { get; set; }
        public string SeatColumn { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
