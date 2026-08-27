using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    // Lets a pet have MULTIPLE gallery photos, separate from Pet.ImageUrl
    // (which stays as the single "cover" photo shown on lists/cards).
    public class PetImage
    {
        [Key]
        public int PetImageId { get; set; }

        [Required]
        public int PetId { get; set; }

        [ForeignKey("PetId")]
        public Pet Pet { get; set; } = null!;

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public DateTime UploadedDate { get; set; } = DateTime.Now;
    }
}
