using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    // A single user's rating + comment against a Veterinarian, Shelter, or
    // Product. TargetType/TargetId keep this generic instead of needing a
    // separate table per rateable entity (matches the SRS's "Feedback and
    // Ratings" common feature: rate veterinarians, shelters, or products).
    public class Rating
    {
        [Key]
        public int RatingId { get; set; }

        [Required]
        [StringLength(20)]
        public string TargetType { get; set; } = string.Empty; // "Product" | "Veterinarian" | "Shelter"

        [Required]
        public int TargetId { get; set; }

        [Required]
        public string RaterUserId { get; set; } = string.Empty;

        [ForeignKey("RaterUserId")]
        public ApplicationUser? Rater { get; set; }

        [Range(1, 5)]
        public int Score { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
