namespace THSR.Api.Models
{
    public class Train
    {
        public int TrainID { get; set; }
        public string TrainNo { get; set; } = null!;
        public int DepartureStationID { get; set; }
        public int ArrivalStationID { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int TotalSeats { get; set; }

        public Station? DepartureStation { get; set; }
        public Station? ArrivalStation { get; set; }
    }
}
