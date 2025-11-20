namespace THSRBooking.Api.Models
{
    public class OrderCreateResponse
    {
        public long OrderId { get; set; }
        public string OrderNumber { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime TravelDate { get; set; }
        public string TrainNo { get; set; } = "";
        public string FromStationName { get; set; } = "";
        public string ToStationName { get; set; } = "";
        public decimal TotalAmount { get; set; }

        public List<OrderTicketItem> Tickets { get; set; } = new();
    }

    public class OrderTicketItem
    {
        public string PassengerName { get; set; } = "";
        public string PassengerType { get; set; } = "";
        public int CarNumber { get; set; }
        public int SeatRow { get; set; }
        public string SeatColumn { get; set; } = "";
        public decimal Price { get; set; }
    }
}
