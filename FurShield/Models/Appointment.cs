using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int PetId { get; set; }

        [ForeignKey("PetId")]
        public Pet Pet { get; set; } = null!;

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [ForeignKey("OwnerId")]
        public ApplicationUser Owner { get; set; } = null!;

        [Required]
        public int VeterinarianId { get; set; }

        [ForeignKey("VeterinarianId")]
        public Veterinarian Veterinarian { get; set; } = null!;

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string? Reason { get; set; }

        public string? Notes { get; set; }
    }
}