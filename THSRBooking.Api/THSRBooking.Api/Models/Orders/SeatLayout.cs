using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THSRBooking.Api.Models
{
    [Table("SeatLayouts")]
    public class SeatLayout
    {
        [Key]
        public int SeatLayoutId { get; set; }

        public int CarNumber { get; set; }
        public int SeatRow { get; set; }

        [Required]
        [MaxLength(1)]
        public string SeatColumn { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }
        public bool IsWindow { get; set; }
        public bool IsAisle { get; set; }
    }
}
