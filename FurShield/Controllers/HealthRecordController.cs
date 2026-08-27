using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    [Authorize(Roles = "PetOwner")]
    public class HealthRecordController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HealthRecordController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

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

        // GET: HealthRecord/Index
        public async Task<IActionResult> Index(int? petId)
        {
            var userId = _userManager.GetUserId(User);
            var familyUserIds = await GetFamilyUserIdsAsync(userId!);

            var recordsQuery = _context.HealthRecords
                .Include(h => h.Pet)
                .ThenInclude(p => p.Owner)
                .Include(h => h.Veterinarian)
                .ThenInclude(v => v.User)
                .Where(h => familyUserIds.Contains(h.Pet.OwnerId));

            if (petId.HasValue)
            {
                recordsQuery = recordsQuery.Where(h => h.PetId == petId.Value);
                ViewBag.PetId = petId.Value;
            }

            ViewBag.IsInFamily = familyUserIds.Count > 1;
            ViewBag.CurrentUserId = userId;

            return View(await recordsQuery.OrderByDescending(h => h.VisitDate).ToListAsync());
        }

        // GET: HealthRecord/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var familyUserIds = await GetFamilyUserIdsAsync(userId!);

            var healthRecord = await _context.HealthRecords
                .Include(h => h.Pet)
                .ThenInclude(p => p.Owner)
                .Include(h => h.Veterinarian)
                .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(m => m.HealthRecordId == id && familyUserIds.Contains(m.Pet.OwnerId));

            if (healthRecord == null) return NotFound();

            ViewBag.IsMine = healthRecord.Pet.OwnerId == userId;

            return View(healthRecord);
        }

        // GET: HealthRecord/Create
        public async Task<IActionResult> Create(int? petId)
        {
            var userId = _userManager.GetUserId(User);
            await PopulateDropdowns(userId, petId);

            var model = new HealthRecord { VisitDate = DateTime.Now };
            if (petId.HasValue) model.PetId = petId.Value;

            return View(model);
        }

        // POST: HealthRecord/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PetId,VeterinarianId,VisitDate,Diagnosis,Symptoms,Treatment,Vaccination,LabResults,Prescription,Notes,NextDueDate")] HealthRecord healthRecord)
        {
            var userId = _userManager.GetUserId(User);
            var familyUserIds = await GetFamilyUserIdsAsync(userId!);

            // Validation 1: Pet selection missing
            if (healthRecord.PetId == 0)
            {
                ModelState.AddModelError("PetId", "Please select a valid pet.");
                await PopulateDropdowns(userId, healthRecord.PetId);
                return View(healthRecord);
            }

            // Validation 2: Check ownership (allows current user OR family members' pets)
            var ownsPet = await _context.Pets.AnyAsync(p => p.PetId == healthRecord.PetId && familyUserIds.Contains(p.OwnerId));
            if (!ownsPet) return Forbid();

            ModelState.Remove("Pet");
            ModelState.Remove("Veterinarian");

            if (ModelState.IsValid)
            {
                healthRecord.AddedByOwner = true;

                // Check if NextDueDate is set and within reminder window
                if (healthRecord.NextDueDate.HasValue && healthRecord.NextDueDate.Value.Date <= DateTime.Now.Date.AddDays(7))
                {
                    healthRecord.DueReminderSent = true;
                    var pet = await _context.Pets.FindAsync(healthRecord.PetId);
                    if (pet != null)
                    {
                        var isOverdue = healthRecord.NextDueDate.Value.Date < DateTime.Now.Date;
                        var dateText = healthRecord.NextDueDate.Value.ToString("dd MMM yyyy");
                        var vaccineInfo = !string.IsNullOrWhiteSpace(healthRecord.Vaccination) ? $" ({healthRecord.Vaccination})" : "";
                        var msg = isOverdue
                            ? $"Reminder: {pet.Name}'s vaccination/follow-up{vaccineInfo} was due on {dateText}. Please schedule a vet visit soon."
                            : $"Upcoming Reminder: {pet.Name}'s vaccination/follow-up{vaccineInfo} is scheduled for {dateText}.";

                        _context.Notifications.Add(new AppNotification
                        {
                            UserId = pet.OwnerId,
                            Message = msg,
                            Link = "/HealthRecord/Index",
                            DateCreated = DateTime.Now,
                            IsRead = false
                        });
                    }
                }
                else
                {
                    healthRecord.DueReminderSent = false;
                }

                _context.HealthRecords.Add(healthRecord);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(userId, healthRecord.PetId);
            return View(healthRecord);
        }

        // GET: HealthRecord/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            var healthRecord = await _context.HealthRecords
                .FirstOrDefaultAsync(h => h.HealthRecordId == id && h.Pet.OwnerId == userId && h.AddedByOwner);

            if (healthRecord == null) return NotFound();

            await PopulateDropdowns(userId, healthRecord.PetId);
            return View(healthRecord);
        }

        // POST: HealthRecord/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("HealthRecordId,PetId,VeterinarianId,VisitDate,Diagnosis,Symptoms,Treatment,Vaccination,LabResults,Prescription,Notes,NextDueDate")] HealthRecord healthRecord)
        {
            if (id != healthRecord.HealthRecordId) return NotFound();

            var userId = _userManager.GetUserId(User);
            var familyUserIds = await GetFamilyUserIdsAsync(userId!);

            var existing = await _context.HealthRecords
                .FirstOrDefaultAsync(h => h.HealthRecordId == id && h.Pet.OwnerId == userId && h.AddedByOwner);
            if (existing == null) return NotFound();

            var ownsPet = await _context.Pets.AnyAsync(p => p.PetId == healthRecord.PetId && familyUserIds.Contains(p.OwnerId));
            if (!ownsPet) return Forbid();

            ModelState.Remove("Pet");
            ModelState.Remove("Veterinarian");

            if (ModelState.IsValid)
            {
                existing.PetId = healthRecord.PetId;
                existing.VeterinarianId = healthRecord.VeterinarianId;
                existing.VisitDate = healthRecord.VisitDate;
                existing.Diagnosis = healthRecord.Diagnosis;
                existing.Symptoms = healthRecord.Symptoms;
                existing.Treatment = healthRecord.Treatment;
                existing.Vaccination = healthRecord.Vaccination;
                existing.LabResults = healthRecord.LabResults;
                existing.Prescription = healthRecord.Prescription;
                existing.Notes = healthRecord.Notes;

                // Due date change handling
                if (existing.NextDueDate != healthRecord.NextDueDate)
                {
                    existing.NextDueDate = healthRecord.NextDueDate;
                    if (existing.NextDueDate.HasValue && existing.NextDueDate.Value.Date <= DateTime.Now.Date.AddDays(7))
                    {
                        existing.DueReminderSent = true;
                        var pet = await _context.Pets.FindAsync(existing.PetId);
                        if (pet != null)
                        {
                            var isOverdue = existing.NextDueDate.Value.Date < DateTime.Now.Date;
                            var dateText = existing.NextDueDate.Value.ToString("dd MMM yyyy");
                            var vaccineInfo = !string.IsNullOrWhiteSpace(existing.Vaccination) ? $" ({existing.Vaccination})" : "";
                            var msg = isOverdue
                                ? $"Reminder: {pet.Name}'s vaccination/follow-up{vaccineInfo} was due on {dateText}. Please schedule a vet visit soon."
                                : $"Upcoming Reminder: {pet.Name}'s vaccination/follow-up{vaccineInfo} is scheduled for {dateText}.";

                            _context.Notifications.Add(new AppNotification
                            {
                                UserId = pet.OwnerId,
                                Message = msg,
                                Link = $"/HealthRecord/Details/{existing.HealthRecordId}",
                                DateCreated = DateTime.Now,
                                IsRead = false
                            });
                        }
                    }
                    else
                    {
                        existing.DueReminderSent = false;
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = existing.HealthRecordId });
            }

            await PopulateDropdowns(userId, healthRecord.PetId);
            return View(healthRecord);
        }

        private async Task PopulateDropdowns(string userId, int? selectedPetId)
        {
            var familyUserIds = await GetFamilyUserIdsAsync(userId);

            var pets = await _context.Pets
                .Where(p => familyUserIds.Contains(p.OwnerId))
                .OrderBy(p => p.Name)
                .ToListAsync();

            var vets = await _context.Veterinarians
                .Include(v => v.User)
                .Select(v => new
                {
                    v.VeterinarianId,
                    DoctorName = v.User != null ? v.User.UserName : "Dr. " + v.Specialization
                })
                .ToListAsync();

            ViewBag.PetList = new SelectList(pets, "PetId", "Name", selectedPetId);
            ViewBag.VetList = new SelectList(vets, "VeterinarianId", "DoctorName");
        }
    }
}