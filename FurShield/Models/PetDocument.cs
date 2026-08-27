using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    public class PetDocument
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        public int PetId { get; set; }

        [ForeignKey("PetId")]
        public Pet Pet { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        // Groups documents (Insurance, Vaccination Certificate, X-Ray, Lab
        // Report, Other) so Insurance papers get their own dedicated section
        // instead of being mixed in with every other document type.
        [StringLength(50)]
        public string DocumentType { get; set; } = "Other";
    }
}