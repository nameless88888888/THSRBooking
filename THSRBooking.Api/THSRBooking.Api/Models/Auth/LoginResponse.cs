namespace THSRBooking.Api.Models
{
    public class LoginResponse
    {
        public string Token { get; set; } = "";
        public DateTime ExpireAt { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = "";   // 顯示給前端使用者
        public string Email { get; set; } = "";
    }
}
