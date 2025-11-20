// Models/Trains/TrainSearchRequest.cs
namespace THSRBooking.Api.Models
{
    public class TrainSearchRequest
    {
        public int FromStationId { get; set; }
        public int ToStationId { get; set; }
        public DateTime TravelDate { get; set; }
        public TimeSpan? TimeFrom { get; set; }
        public TimeSpan? TimeTo { get; set; }
    }
}
