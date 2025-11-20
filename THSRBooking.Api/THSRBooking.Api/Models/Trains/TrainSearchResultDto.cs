namespace THSRBooking.Api.Models
{
    public class TrainSearchResultDto
    {
        public int TrainId { get; set; }
        public string TrainNo { get; set; } = "";
        public DateTime DepartDateTime { get; set; }
        public DateTime ArriveDateTime { get; set; }
        /// <summary>
        /// 行車時間（分鐘），已保證為正數
        /// </summary>
        public int DurationMinutes { get; set; }
    }
}
