namespace THSRBooking.Api.Models
{
    public class Order
    {
        public int OrderId { get; set; }      // ← 原本 int 改成 long

        public int UserId { get; set; }
        public string OrderNumber { get; set; } = "";

        public int FromStationId { get; set; }
        public int ToStationId { get; set; }
        public DateTime TravelDate { get; set; }

        public int TrainId { get; set; }
        public DateTime? DepartDateTime { get; set; }
        public DateTime? ArriveDateTime { get; set; }

        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Confirmed";
        public DateTime CreatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
}
