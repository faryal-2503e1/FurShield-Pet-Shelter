using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    // A recurring weekly slot during which a vet is available for appointments
    // (e.g. "Monday 9:00 AM - 1:00 PM"). Vets manage these from their profile.
    public class VetTimeSlot
    {
        [Key]
        public int VetTimeSlotId { get; set; }

        [Required]
        public int VeterinarianId { get; set; }

        [ForeignKey("VeterinarianId")]
        public Veterinarian Veterinarian { get; set; } = null!;

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }
    }
}
