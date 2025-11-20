namespace THSRBooking.Api.Models
{
    public partial class SeatReservation
    {
        public int SeatReservationId { get; set; }   // PK 還是 int，因為 DB 現在是 INT IDENTITY

        public int TrainId { get; set; }
        public DateTime TravelDate { get; set; }
        public int FromStationId { get; set; }
        public int ToStationId { get; set; }
        public int CarNumber { get; set; }
        public int SeatRow { get; set; }
        public string SeatColumn { get; set; } = string.Empty;

        public int OrderTicketId { get; set; }       // 這個我們剛剛讓 DB 也是 INT
        public bool IsCancelled { get; set; }
        public int OrderId { get; set; }            // ← 這裡改成 long（對應 Orders.OrderId）
    }
}
