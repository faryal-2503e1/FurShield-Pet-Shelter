using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    [Authorize(Roles = "Shelter")]
    public class AdoptionController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdoptionController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. List All Adoptable Pets
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);

            if (shelter == null) return RedirectToAction("Index", "Shelter");

            var listings = await _context.AdoptionListings
                .Where(a => a.ShelterId == shelter.ShelterId)
                .ToListAsync();

            return View(listings);
        }

        // 2. GET: Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdoptionListing model, IFormFile? PetImage)
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);

            if (shelter == null) return RedirectToAction("Index", "Shelter");

            // Ignore navigation & auto properties for ModelState validation
            ModelState.Remove("Shelter");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                if (PetImage != null && PetImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + PetImage.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await PetImage.CopyToAsync(fileStream);
                    }

                    model.ImageUrl = "/uploads/" + uniqueFileName;
                }

                model.ShelterId = shelter.ShelterId;
                model.Status = "Available";

                _context.AdoptionListings.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Adoption/Interests - review adoption requests from pet owners
        public async Task<IActionResult> Interests()
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shelter == null) return RedirectToAction("Index", "Shelter");

            var interests = await _context.AdoptionInterests
                .Include(i => i.AdoptionListing)
                .Include(i => i.Owner)
                .Where(i => i.AdoptionListing.ShelterId == shelter.ShelterId)
                .OrderByDescending(i => i.DateSubmitted)
                .ToListAsync();

            return View(interests);
        }

        // POST: Adoption/RespondInterest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondInterest(int interestId, string status, string? response)
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shelter == null) return RedirectToAction("Index", "Shelter");

            var interest = await _context.AdoptionInterests
                .Include(i => i.AdoptionListing)
                .FirstOrDefaultAsync(i => i.AdoptionInterestId == interestId && i.AdoptionListing.ShelterId == shelter.ShelterId);

            if (interest == null) return NotFound();

            interest.Status = status; // "Approved" or "Rejected"
            interest.ShelterResponse = response;
            _context.AdoptionInterests.Update(interest);

            string notifyMessage;
            if (status == "Approved")
            {
                // 1. Mark pet as Adopted
                interest.AdoptionListing.Status = "Adopted";
                _context.AdoptionListings.Update(interest.AdoptionListing);

                // 2. TRANSFER PET TO OWNER'S "MY PETS" TABLE
                var transferredPet = new Pet
                {
                    Name = interest.AdoptionListing.PetName,
                    Species = interest.AdoptionListing.Species,
                    Breed = interest.AdoptionListing.Breed,
                    Age = interest.AdoptionListing.Age,
                    Gender = interest.AdoptionListing.Gender,
                    ImageUrl = interest.AdoptionListing.ImageUrl,
                    MedicalHistory = interest.AdoptionListing.HealthStatus,
                    OwnerId = interest.OwnerId // Owner set ho gaya
                };
                _context.Pets.Add(transferredPet);

                // 3. Auto-reject other pending requests
                var otherPending = await _context.AdoptionInterests
                    .Where(i => i.AdoptionListingId == interest.AdoptionListingId
                                && i.AdoptionInterestId != interestId
                                && i.Status == "Pending")
                    .ToListAsync();

                foreach (var other in otherPending)
                {
                    other.Status = "Rejected";
                    other.ShelterResponse = "This pet has been adopted by another user.";
                    _context.Notifications.Add(new AppNotification
                    {
                        UserId = other.OwnerId,
                        Message = $"Your request to adopt {interest.AdoptionListing.PetName} was not approved.",
                        Link = "/Adopt/MyInterests",
                        DateCreated = DateTime.Now
                    });
                }
                _context.AdoptionInterests.UpdateRange(otherPending);

                notifyMessage = $"Great news! Your request to adopt {interest.AdoptionListing.PetName} was approved and added to your 'My Pets'!";
            }
            else
            {
                notifyMessage = $"Your request to adopt {interest.AdoptionListing.PetName} was not approved.";
            }

            _context.Notifications.Add(new AppNotification
            {
                UserId = interest.OwnerId,
                Message = notifyMessage,
                Link = "/Adopt/MyInterests",
                DateCreated = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Response sent to the pet owner.";
            return RedirectToAction(nameof(Interests));
        }

        // 3. GET: Edit
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shelter == null) return NotFound();

            var listing = await _context.AdoptionListings
                .FirstOrDefaultAsync(a => a.AdoptionListingId == id && a.ShelterId == shelter.ShelterId);
            if (listing == null) return NotFound();

            return View(listing);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdoptionListing model, IFormFile? PetImage)
        {
            if (id != model.AdoptionListingId) return NotFound();

            ModelState.Remove("Shelter");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);
                if (shelter == null) return NotFound();

                var existingListing = await _context.AdoptionListings
                    .FirstOrDefaultAsync(a => a.AdoptionListingId == id && a.ShelterId == shelter.ShelterId);
                if (existingListing == null) return NotFound();

                if (PetImage != null && PetImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + PetImage.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await PetImage.CopyToAsync(fileStream);
                    }

                    existingListing.ImageUrl = "/uploads/" + uniqueFileName;
                }

                existingListing.PetName = model.PetName;
                existingListing.Species = model.Species;
                existingListing.Breed = model.Breed;
                existingListing.Age = model.Age;
                existingListing.Gender = model.Gender;
                existingListing.HealthStatus = model.HealthStatus;
                existingListing.Status = model.Status;

                _context.AdoptionListings.Update(existingListing);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // 4. Delete Listing
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shelter == null) return NotFound();

            var listing = await _context.AdoptionListings
                .FirstOrDefaultAsync(a => a.AdoptionListingId == id && a.ShelterId == shelter.ShelterId);
            if (listing != null)
            {
                _context.AdoptionListings.Remove(listing);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}