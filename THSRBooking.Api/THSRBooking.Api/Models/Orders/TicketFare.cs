using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using THSRBooking.Api.Models;

[Table("TicketFares")]
public class TicketFare
{
    [Key]
    public int FareId { get; set; }

    public int FromStationId { get; set; }
    public int ToStationId { get; set; }

    // ─────────── 票價區 ───────────
    public int BaseFare { get; set; }
    public int AdultPrice { get; set; }
    public int ChildPrice { get; set; }
    public int SeniorPrice { get; set; }
    public int LovePrice { get; set; }
    public int FreeChildPrice { get; set; }

    // ─────────── Navigation ───────────
    public Station FromStation { get; set; }
    public Station ToStation { get; set; }
}
