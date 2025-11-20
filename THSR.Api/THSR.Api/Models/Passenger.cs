namespace THSR.Api.Models
{
    public class Passenger
    {
        public int PassengerID { get; set; }
        public int? UserID { get; set; }
        public string Name { get; set; } = null!;
        public int? Age { get; set; }
        public string? IDNumber { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
