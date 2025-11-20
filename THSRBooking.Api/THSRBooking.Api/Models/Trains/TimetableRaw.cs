namespace THSRBooking.Api.Models
{
    // 用來查星期幾有開車，無需完整欄位，只要我們會用到的
    public class TimetableRaw
    {
        public int TrainNo { get; set; }
        public string Direction { get; set; } = "";
        public TimeSpan? DepartureTime { get; set; }
        public TimeSpan? ArrivalTime { get; set; }

        public string? Mon { get; set; }
        public string? Tue { get; set; }
        public string? Wed { get; set; }
        public string? Thu { get; set; }
        public string? Fri { get; set; }
        public string? Sat { get; set; }
        public string? Sun { get; set; }

        // 如果要用到各站時間再補
    }
}
