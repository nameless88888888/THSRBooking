namespace THSR.Api.Models
{
    public class CreateOrderRequest
    {
        public int UserId { get; set; }          // 從登入使用者來
        public int TrainId { get; set; }         // 要訂的那班車

        public string PassengerName { get; set; } = null!;
        public string? PassengerPhone { get; set; }
        public string? PassengerIdNumber { get; set; }
        public string? SeatNumber { get; set; }  // 先用文字欄位
    }
}
