namespace THSR.Api.Models
{
    // 訂單列表用
    public class OrderListItem
    {
        public int OrderId { get; set; }
        public DateTime OrderTime { get; set; }
        public string Status { get; set; } = null!;
        public int TotalAmount { get; set; }

        public string TrainNo { get; set; } = null!;
        public string DepartureStation { get; set; } = null!;
        public string ArrivalStation { get; set; } = null!;
        public DateTime DepartureTime { get; set; }
    }

    // 訂單明細用：一張票
    public class OrderTicketItem
    {
        public int TicketId { get; set; }
        public string PassengerName { get; set; } = null!;
        public string? SeatNumber { get; set; }
        public int Price { get; set; }
        public string TicketType { get; set; } = null!;
        public string Status { get; set; } = null!;
    }

    // 訂單明細用：整張訂單
    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public DateTime OrderTime { get; set; }
        public string Status { get; set; } = null!;
        public int TotalAmount { get; set; }

        public string TrainNo { get; set; } = null!;
        public string DepartureStation { get; set; } = null!;
        public string ArrivalStation { get; set; } = null!;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }

        public List<OrderTicketItem> Tickets { get; set; } = new();
    }
}
