using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    [Authorize(Roles = "Shelter")]
    public class ShelterProductController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShelterProductController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ShelterProduct/Index (Shelter Inventory List)
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shelter == null) return RedirectToAction("Index", "Shelter");

            var products = await _context.Products
                .Where(p => p.ShelterId == shelter.ShelterId)
                .ToListAsync();

            return View(products);
        }

        // GET: ShelterProduct/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ShelterProduct/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? ProductImage)
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shelter == null) return RedirectToAction("Index", "Shelter");

            ModelState.Remove("Shelter");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                if (ProductImage != null && ProductImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + ProductImage.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProductImage.CopyToAsync(fileStream);
                    }

                    product.ImageUrl = "/uploads/products/" + uniqueFileName;
                }

                product.ShelterId = shelter.ShelterId;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // New product arrival notification -> saare Pet Owners ko in-app alert
                await NotifyOwnersOfNewProductAsync(product);

                TempData["SuccessMessage"] = "Product added successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        // GET: ShelterProduct/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shelter == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id && p.ShelterId == shelter.ShelterId);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: ShelterProduct/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? ProductImage)
        {
            if (id != model.ProductId) return NotFound();

            ModelState.Remove("Shelter");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);
                if (shelter == null) return NotFound();

                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == id && p.ShelterId == shelter.ShelterId);

                if (existingProduct == null) return NotFound();

                if (ProductImage != null && ProductImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + ProductImage.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProductImage.CopyToAsync(fileStream);
                    }

                    existingProduct.ImageUrl = "/uploads/products/" + uniqueFileName;
                }

                existingProduct.Name = model.Name;
                existingProduct.Category = model.Category;
                existingProduct.Price = model.Price;
                existingProduct.StockQuantity = model.StockQuantity;
                existingProduct.Description = model.Description;

                _context.Products.Update(existingProduct);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // POST: ShelterProduct/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shelter == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id && p.ShelterId == shelter.ShelterId);

            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper: naya product add hone par saare registered Pet Owners ko
        // "new product arrival" in-app notification bhejta hai (SRS 1.6 - Notifications).
        [NonAction]
        private async Task NotifyOwnersOfNewProductAsync(Product product)
        {
            var petOwners = await _userManager.GetUsersInRoleAsync("PetOwner");
            if (!petOwners.Any()) return;

            var notifications = petOwners.Select(owner => new AppNotification
            {
                UserId = owner.Id,
                Message = $"New product arrival: \"{product.Name}\" is now available in the {product.Category} category!",
                Link = $"/Product/Details/{product.ProductId}",
                DateCreated = DateTime.Now
            });

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }
    }
}