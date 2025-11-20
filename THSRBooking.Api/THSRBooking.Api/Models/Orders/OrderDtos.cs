using System.ComponentModel.DataAnnotations;

namespace THSRBooking.Api.Models
{
    // 建立訂單時前端送進來的格式
    public class OrderCreateRequest
    {
        [Required]
        public int TrainId { get; set; }

        [Required]
        public DateTime TravelDate { get; set; }

        [Required]
        public int FromStationId { get; set; }

        [Required]
        public int ToStationId { get; set; }

        [Required]
        public List<OrderPassengerDto> Passengers { get; set; } = new();
    }

    public class OrderPassengerDto
    {
        [Required]
        public string PassengerName { get; set; } = "";

        [Required]
        public string PassengerType { get; set; } = "Adult"; // Adult / Child ... 看你之後要不要做

        [Required]
        public int CarNumber { get; set; }

        [Required]
        public int SeatRow { get; set; }

        [Required]
        public string SeatColumn { get; set; } = "";   // A/B/C/D
    }

    // 回傳給前端看的訂單摘要（給 GET /api/orders/my 用）
    public class OrderSummaryDto
    {
        public int OrderId { get; set; }
        public string TrainNo { get; set; } = "";
        public DateTime TravelDate { get; set; }

        public string FromStationName { get; set; } = "";
        public string ToStationName { get; set; } = "";

        public int PassengerCount { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string PassengerSeats { get; set; } = "";
        public decimal TotalAmount { get; set; }
    }
}
