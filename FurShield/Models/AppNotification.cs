using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    // A simple in-app notification (e.g. "new adoption interest", "your
    // request was approved"). Shown to whichever user it belongs to.
    public class AppNotification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        // Where "View" should take the user (e.g. /Adoption/Interests).
        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
