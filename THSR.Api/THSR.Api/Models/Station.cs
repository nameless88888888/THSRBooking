using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THSR.Api.Models
{
    
    public class Station
    {
        [Key]
        public int StationID { get; set; }

        [Required]
        [MaxLength(50)]
        public string StationName { get; set; }

        [MaxLength(100)]
        public string Location { get; set; }
    }
}
