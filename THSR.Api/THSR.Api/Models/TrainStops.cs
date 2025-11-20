using System.ComponentModel.DataAnnotations;

public class TrainStops
{
    [Key]
    public int TrainStopID { get; set; }
    public int TrainID { get; set; }
    public int StationID { get; set; }
    public int StopOrder { get; set; }
    public DateTime ArrivalTime { get; set; }
    public DateTime DepartureTime { get; set; }
}
