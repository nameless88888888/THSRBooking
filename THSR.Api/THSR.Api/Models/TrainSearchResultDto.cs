public class TrainSearchResultDto
{
    public int TrainID { get; set; }
    public int TrainNo { get; set; }
    public string DepartureStation { get; set; } = string.Empty;
    public string ArrivalStation { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int TotalSeats { get; set; }
    public int RemainingSeats { get; set; }
}
