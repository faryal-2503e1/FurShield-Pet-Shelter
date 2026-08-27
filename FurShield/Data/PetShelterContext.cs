using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FurShield.Data
{
    public class PetShelterContext : IdentityDbContext<ApplicationUser>
    {
        public PetShelterContext(DbContextOptions<PetShelterContext> options)
            : base(options) { }

        // Application DbSets (Tables)
        public DbSet<Pet> Pets { get; set; }
        public DbSet<PetDocument> PetDocuments { get; set; }
        public DbSet<Veterinarian> Veterinarians { get; set; }
        public DbSet<HealthRecord> HealthRecords { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Shelter> Shelters { get; set; }
        public DbSet<AdoptionListing> AdoptionListings { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<PetImage> PetImages { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<VetTimeSlot> VetTimeSlots { get; set; }
        public DbSet<AdoptionInterest> AdoptionInterests { get; set; }
        public DbSet<AppNotification> Notifications { get; set; }
        public DbSet<CareStatusLog> CareStatusLogs { get; set; }
        public DbSet<FamilyAccount> FamilyAccounts { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<FurShield.Models.ContactMessage> ContactMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Unique constraints
            builder.Entity<Veterinarian>().HasIndex(v => v.UserId).IsUnique();
            builder.Entity<Shelter>().HasIndex(s => s.UserId).IsUnique();
            builder.Entity<FamilyAccount>().ToTable("FamilyAccount");

            // Prevent Cascade Delete Cycles for Appointments
            builder.Entity<Appointment>()
                .HasOne(a => a.Owner)
                .WithMany(u => u.Appointments)
                .HasForeignKey(a => a.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.Veterinarian)
                .WithMany(v => v.Appointments)
                .HasForeignKey(a => a.VeterinarianId)
                .OnDelete(DeleteBehavior.Restrict);

            // FIX: HealthRecord Cascade Delete Cycle Ko Rokein
            builder.Entity<HealthRecord>()
                .HasOne(h => h.Veterinarian)
                .WithMany(v => v.HealthRecords)
                .HasForeignKey(h => h.VeterinarianId)
                .OnDelete(DeleteBehavior.Restrict);

            // FIX: AdoptionInterest ka OwnerId seedha AspNetUsers se juda hai, aur
            // AspNetUsers -> Shelters -> AdoptionListings -> AdoptionInterests wala
            // rasta pehle se cascade hai. Dono paths se SQL Server "multiple cascade
            // paths" error deta hai - is liye OwnerId wale link ko Restrict kar dein.
            builder.Entity<AdoptionInterest>()
                .HasOne(ai => ai.Owner)
                .WithMany()
                .HasForeignKey(ai => ai.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Same reasoning as AdoptionInterest above: Rating -> ApplicationUser
            // is a direct link alongside other cascade paths, so restrict it.
            builder.Entity<Rating>()
                .HasOne(r => r.Rater)
                .WithMany()
                .HasForeignKey(r => r.RaterUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}