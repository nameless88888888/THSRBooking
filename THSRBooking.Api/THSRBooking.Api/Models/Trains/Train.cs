namespace THSRBooking.Api.Models
{
    public class Train
    {
        public int TrainId { get; set; }
        public string TrainNo { get; set; } = "";
        public bool Direction { get; set; }  // 0: 北上, 1: 南下
        public bool IsActive { get; set; }
    }
}
