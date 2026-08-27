using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    public class Shelter
    {
        [Key]
        public int ShelterId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string ShelterName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string ContactPerson { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        public string? Description { get; set; }

        public ICollection<AdoptionListing> AdoptionListings { get; set; } = new List<AdoptionListing>();
    }
}