namespace THSRBooking.Api.Models
{
    public class TrainStop
    {
        public long TrainStopId { get; set; }
        public int TrainId { get; set; }
        public int StationId { get; set; }
        public int StopOrder { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public TimeSpan? DepartureTime { get; set; }
    }
}
