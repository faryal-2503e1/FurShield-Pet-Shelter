using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    public class HealthRecord
    {
        [Key]
        public int HealthRecordId { get; set; }

        [Required]
        public int PetId { get; set; }

        [ForeignKey("PetId")]
        public Pet Pet { get; set; } = null!;

        [Required]
        public int VeterinarianId { get; set; }

        [ForeignKey("VeterinarianId")]
        public Veterinarian Veterinarian { get; set; } = null!;

        [Required]
        public DateTime VisitDate { get; set; }

        [StringLength(500)]
        public string? Diagnosis { get; set; }

        [StringLength(500)]
        public string? Symptoms { get; set; }

        [StringLength(1000)]
        public string? Treatment { get; set; }

        [StringLength(500)]
        public string? Vaccination { get; set; }

        [StringLength(1000)]
        public string? LabResults { get; set; }

        [StringLength(1000)]
        public string? Prescription { get; set; }

        public string? Notes { get; set; }

        // True when the pet owner logged this entry themselves (allergy note, home
        // observation, etc.) instead of a veterinarian creating it during a visit.
        // Owners can only edit records where this is true.
        public bool AddedByOwner { get; set; } = false;

        // Optional: when the vet sets a follow-up/next vaccination date, the
        // reminder background service notifies the pet owner as it approaches.
        [Display(Name = "Next Vaccination/Follow-up Due Date")]
        public DateTime? NextDueDate { get; set; }

        // Set true once the reminder notification has been sent for
        // NextDueDate, so the background service doesn't notify twice.
        public bool DueReminderSent { get; set; } = false;
    }
}