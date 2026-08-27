using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    // A pet owner's request to adopt a specific shelter listing, plus the
    // shelter's response - this is the "coordinate with adopters" workflow.
    public class AdoptionInterest
    {
        [Key]
        public int AdoptionInterestId { get; set; }

        [Required]
        public int AdoptionListingId { get; set; }

        [ForeignKey("AdoptionListingId")]
        public AdoptionListing AdoptionListing { get; set; } = null!;

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [ForeignKey("OwnerId")]
        public ApplicationUser Owner { get; set; } = null!;

        [StringLength(1000)]
        public string? Message { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [StringLength(1000)]
        public string? ShelterResponse { get; set; }

        public DateTime DateSubmitted { get; set; } = DateTime.Now;
    }
}
