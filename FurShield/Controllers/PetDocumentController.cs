using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    [Authorize(Roles = "PetOwner")]
    public class PetDocumentController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public PetDocumentController(PetShelterContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // Returns the current user's own Id, plus every other member of their
        // family group (if they belong to one) — same pattern used across
        // AppointmentController / HealthRecordController / OrderController.
        private async Task<List<string>> GetFamilyUserIdsAsync(string currentUserId)
        {
            var currentUser = await _context.Users.FindAsync(currentUserId);

            if (currentUser?.FamilyAccountId != null)
            {
                return await _context.Users
                    .Where(u => u.FamilyAccountId == currentUser.FamilyAccountId)
                    .Select(u => u.Id)
                    .ToListAsync();
            }

            return new List<string> { currentUserId };
        }

        // GET: PetDocument/Index/5 (Pet ID ke hisab se documents view karna)
        public async Task<IActionResult> Index(int? petId)
        {
            var userId = _userManager.GetUserId(User);
            var familyUserIds = await GetFamilyUserIdsAsync(userId!);

            var documentsQuery = _context.PetDocuments
                .Include(d => d.Pet)
                .Where(d => familyUserIds.Contains(d.Pet.OwnerId));

            if (petId.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.PetId == petId.Value);
                ViewBag.PetId = petId.Value;
            }

            ViewBag.IsInFamily = familyUserIds.Count > 1;

            return View(await documentsQuery.ToListAsync());
        }

        // Fixed set of document categories - keeps Insurance papers grouped
        // separately from vet certificates/X-rays/lab reports.
        private static readonly List<string> DocumentTypes = new()
        {
            "Insurance", "Vaccination Certificate", "X-Ray", "Lab Report", "Vet Certificate", "Other"
        };

        // GET: PetDocument/Upload
        public async Task<IActionResult> Upload(int? petId, string? documentType)
        {
            var userId = _userManager.GetUserId(User);
            var userPets = await _context.Pets.Where(p => p.OwnerId == userId).ToListAsync();

            ViewBag.PetId = new SelectList(userPets, "PetId", "Name", petId);
            ViewBag.DocumentTypes = new SelectList(DocumentTypes, documentType ?? "Other");
            return View();
        }

        // POST: PetDocument/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int petId, string title, string documentType, IFormFile file)
        {
            var userId = _userManager.GetUserId(User);

            // Make sure the pet actually belongs to the logged-in owner before attaching a document to it.
            var ownsPet = await _context.Pets.AnyAsync(p => p.PetId == petId && p.OwnerId == userId);
            if (!ownsPet)
            {
                return Forbid();
            }

            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var document = new PetDocument
                {
                    PetId = petId,
                    Title = title,
                    FilePath = "/uploads/documents/" + uniqueFileName,
                    UploadedDate = DateTime.Now,
                    DocumentType = string.IsNullOrEmpty(documentType) ? "Other" : documentType
                };

                _context.Add(document);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { petId = petId });
            }

            ModelState.AddModelError("", "Please select a valid file.");
            ViewBag.PetId = new SelectList(await _context.Pets.Where(p => p.OwnerId == userId).ToListAsync(), "PetId", "Name", petId);
            ViewBag.DocumentTypes = new SelectList(DocumentTypes, documentType ?? "Other");
            return View();
        }

        // POST: PetDocument/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);

            var document = await _context.PetDocuments
                .Include(d => d.Pet)
                .FirstOrDefaultAsync(d => d.DocumentId == id && d.Pet.OwnerId == userId);

            if (document != null)
            {
                // File ko physical folder se delete karna
                var fullPath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                _context.PetDocuments.Remove(document);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}