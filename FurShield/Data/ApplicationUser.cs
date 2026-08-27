using Microsoft.AspNetCore.Identity;
namespace FurShield.Data;
// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Address { get; set; }

    public int? FamilyAccountId { get; set; }
    public FamilyAccount? FamilyAccount { get; set; }


    // Navigation properties (Nullability fix)
    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
    public virtual Veterinarian? Veterinarian { get; set; }
    public virtual Shelter? Shelter { get; set; }
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
