using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    public class Veterinarian
    {
        [Key]
        public int VeterinarianId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string Specialization { get; set; } = string.Empty;

        public int Experience { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public string? Bio { get; set; }

        public ICollection<HealthRecord> HealthRecords { get; set; } = new List<HealthRecord>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<VetTimeSlot> TimeSlots { get; set; } = new List<VetTimeSlot>();
    }
}