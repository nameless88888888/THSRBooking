using THSRBooking.Api.Models;

public class OrderCreateRequest
{
    public int TrainId { get; set; }
    public string TravelDate { get; set; } = string.Empty;   // ← 改成 string
    public int FromStationId { get; set; }
    public int ToStationId { get; set; }
    public List<OrderPassengerDto> Passengers { get; set; } = new();
}
