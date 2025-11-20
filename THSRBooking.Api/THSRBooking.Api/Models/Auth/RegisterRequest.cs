namespace THSRBooking.Api.Models
{
    public class RegisterRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Name { get; set; } = "";    // 必填
        public string? Phone { get; set; }
    }
}
