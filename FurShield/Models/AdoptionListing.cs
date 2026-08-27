using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    public class AdoptionListing
    {
        [Key]
        public int AdoptionListingId { get; set; }

        [Required]
        public int ShelterId { get; set; }

        [ForeignKey("ShelterId")]
        public Shelter Shelter { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string PetName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Species { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Breed { get; set; }

        public int Age { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(500)]
        public string? HealthStatus { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Available";

        // Used for the "Newest" sort option on the public adoption page.
        public DateTime DateListed { get; set; } = DateTime.Now;

        public ICollection<AdoptionInterest> Interests { get; set; } = new List<AdoptionInterest>();

        // Dated history of care/health status updates (newest first when queried).
        public ICollection<CareStatusLog> CareLogs { get; set; } = new List<CareStatusLog>();
    }
}