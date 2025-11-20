// Models/Trains/TrainSearchResponse.cs
namespace THSRBooking.Api.Models
{
    public class TrainSearchResponse
    {
        public int TrainId { get; set; }
        public string TrainNo { get; set; } = "";
        public DateTime DepartDateTime { get; set; }
        public DateTime ArriveDateTime { get; set; }
        public int DurationMinutes { get; set; }
    }
}
