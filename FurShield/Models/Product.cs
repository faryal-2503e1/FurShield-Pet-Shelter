using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace FurShield.Data
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999)]
        public decimal Price { get; set; }

        public string? Description { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public string? ImageUrl { get; set; }

        // Used for the "Newest" sort option on the shop page, and to know
        // which products are new enough to notify pet owners about.
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Foreign Key for Shelter
        public int? ShelterId { get; set; }
        [ForeignKey("ShelterId")]
        public Shelter? Shelter { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}