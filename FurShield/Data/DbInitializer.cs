using Microsoft.EntityFrameworkCore;

namespace FurShield.Data
{
    public class DbInitializer
    {
        public static async Task SeedAsync(PetShelterContext context)
        {
            // Database ensure created
            await context.Database.EnsureCreatedAsync();

            // Agar pehle se products hain to seed mat karo
            if (await context.Products.AnyAsync())
            {
                return;
            }

            // 8-10 Default Products List
            var products = new List<Product>
            {
                new Product
                {
                    Name = "Pedigree Adult Dry Dog Food 3kg",
                    Category = "Food",
                    Price = 3500.00m,
                    StockQuantity = 25,
                    Description = "High nutrition complete dry food for adult dogs with real chicken flavor.",
                    ImageUrl = "https://images.unsplash.com/photo-1589924691995-400dc9ecc119?w=500"
                },
                new Product
                {
                    Name = "Whiskas Wet Cat Food Pouch (Pack of 12)",
                    Category = "Food",
                    Price = 2400.00m,
                    StockQuantity = 40,
                    Description = "Delicious tuna and salmon wet food pouches for adult cats.",
                    ImageUrl = "https://images.unsplash.com/photo-1568640347023-a616a30bc3bd?w=500"
                },
                new Product
                {
                    Name = "Rubber Chew Bone Toy for Dogs",
                    Category = "Toys",
                    Price = 850.00m,
                    StockQuantity = 15,
                    Description = "Durable non-toxic rubber chew toy ideal for teething puppies and dogs.",
                    ImageUrl = "https://images.unsplash.com/photo-1576201836106-db1758fd1c97?w=500"
                },
                new Product
                {
                    Name = "Interactive Feather Wand Cat Toy",
                    Category = "Toys",
                    Price = 600.00m,
                    StockQuantity = 30,
                    Description = "Engaging feather wand with bell to keep your indoor cats active.",
                    ImageUrl = "https://images.unsplash.com/photo-1615870216519-2f9fa575fa5c?w=500"
                },
                new Product
                {
                    Name = "Anti-Flea & Tick Shampoo 500ml",
                    Category = "Grooming",
                    Price = 1250.00m,
                    StockQuantity = 20,
                    Description = "Gentle cleansing shampoo that eliminates fleas, ticks, and bad odor.",
                    ImageUrl = "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=500"
                },
                new Product
                {
                    Name = "Pet Grooming Slicker Brush",
                    Category = "Grooming",
                    Price = 950.00m,
                    StockQuantity = 18,
                    Description = "Easy clean brush to remove loose fur and tangle for dogs and cats.",
                    ImageUrl = "https://images.unsplash.com/photo-1516734212186-a967f81ad0d7?w=500"
                },
                new Product
                {
                    Name = "Adjustable Padded Dog Harness & Leash",
                    Category = "Accessories",
                    Price = 1800.00m,
                    StockQuantity = 12,
                    Description = "Reflective nylon harness for safe and comfortable night walks.",
                    ImageUrl = "https://images.unsplash.com/photo-1601758228041-f3b2795255f1?w=500"
                },
                new Product
                {
                    Name = "Stainless Steel Double Pet Bowl",
                    Category = "Accessories",
                    Price = 1100.00m,
                    StockQuantity = 22,
                    Description = "Anti-skid base dual bowl for simultaneous food and water serving.",
                    ImageUrl = "https://images.unsplash.com/photo-1541599540903-216a46ca1dc0?w=500"
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
    }
}
    

