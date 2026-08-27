using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    public class Pet
    {
        [Key]
        public int PetId { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [ForeignKey(nameof(OwnerId))]
        public virtual ApplicationUser? Owner { get; set; }

        [Required(ErrorMessage = "Pet name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Species is required.")]
        [StringLength(50)]
        public string Species { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Breed { get; set; }

        [Range(0, 100, ErrorMessage = "Age must be between 0 and 100.")]
        public int Age { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        public string? MedicalHistory { get; set; }

        [Display(Name = "Allergies")]
        [StringLength(500)]
        public string? Allergies { get; set; }

        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Insurance Policy Number")]
        public string? InsurancePolicyNumber { get; set; }

        [Display(Name = "Insurance Details")]
        public string? InsuranceDetails { get; set; }

        // Navigation Properties (Lazy Loading support ke liye 'virtual' add karna best practice hai)
        public virtual ICollection<HealthRecord> HealthRecords { get; set; } = new List<HealthRecord>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<PetDocument> Documents { get; set; } = new List<PetDocument>();
        public virtual ICollection<PetImage> Images { get; set; } = new List<PetImage>();
    }
}