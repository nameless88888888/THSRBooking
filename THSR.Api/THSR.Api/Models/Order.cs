namespace THSR.Api.Models
{
    public class Order
    {
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public DateTime OrderTime { get; set; }
        public int TotalAmount { get; set; }
        public string Status { get; set; } = null!; // Booked / Canceled / Completed
    }
}
