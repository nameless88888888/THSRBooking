namespace THSRBooking.Api.Models.Orders
{
    // OrderDetailDto.cs
    namespace THSRBooking.Api.Models
    {
        public class OrderDetailDto
        {
            public int OrderId { get; set; }
            public string TrainNo { get; set; } = "";
            public DateTime TravelDate { get; set; }
            public string FromStationName { get; set; } = "";
            public string ToStationName { get; set; } = "";
            public DateTime? DepartDateTime { get; set; }
            public DateTime? ArriveDateTime { get; set; }
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }

            public List<OrderPassengerSeatDto> Tickets { get; set; } = new();
        }

        public class OrderPassengerSeatDto
        {
            public string PassengerName { get; set; } = "";
            public string PassengerType { get; set; } = "";
            public int CarNumber { get; set; }
            public int SeatRow { get; set; }
            public string SeatColumn { get; set; } = "";
            public decimal Price { get; set; }
        }
    }

}
