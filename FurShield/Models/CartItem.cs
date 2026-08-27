using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurShield.Data
{
    // One row per (owner, product) sitting in the owner's shopping cart,
    // before it becomes a real Order at checkout.
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [ForeignKey("OwnerId")]
        public ApplicationUser Owner { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        public DateTime DateAdded { get; set; } = DateTime.Now;
    }
}
