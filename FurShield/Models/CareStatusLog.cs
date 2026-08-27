using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    // Keeps a dated history of "Update Pet Care Status" entries for a shelter's
    // adoption listing, instead of overwriting a single text field (SRS: "maintain
    // and update logs").
    public class CareStatusLog
    {
        [Key]
        public int CareStatusLogId { get; set; }

        [Required]
        public int AdoptionListingId { get; set; }

        [ForeignKey("AdoptionListingId")]
        public AdoptionListing AdoptionListing { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string Note { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
