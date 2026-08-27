using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    [Authorize(Roles = "PetOwner")]
    public class AppointmentController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Returns the current user's own Id, plus every other member of their
        // family group (if they belong to one). Used everywhere family-wide
        // visibility is needed - Appointments, Health Records, Orders.
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

        // GET: Appointment/Index
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var familyUserIds = await GetFamilyUserIdsAsync(userId!);

            var appointments = await _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.Owner)
                .Include(a => a.Veterinarian)
                .ThenInclude(v => v.User)
                .Where(a => familyUserIds.Contains(a.OwnerId))
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            ViewBag.CurrentUserId = userId;
            ViewBag.IsInFamily = familyUserIds.Count > 1;

            return View(appointments);
        }

        // GET: Appointment/Create
        public async Task<IActionResult> Create(int? vetId, int? petId)
        {
            var userId = _userManager.GetUserId(User);
            var familyUserIds = await GetFamilyUserIdsAsync(userId!);
            var currentUser = await _userManager.GetUserAsync(User);

            // Any pet belonging to any member of the family can be booked for.
            var sharedPets = await _context.Pets
                .Where(p => familyUserIds.Contains(p.OwnerId))
                .ToListAsync();

            var vets = await _context.Veterinarians.Include(v => v.User).ToListAsync();

            ViewBag.PetId = new SelectList(sharedPets, "PetId", "Name", petId);

            ViewBag.VeterinarianId = new SelectList(
                vets.Select(v => new
                {
                    VeterinarianId = v.VeterinarianId,
                    DisplayName = !string.IsNullOrEmpty(v.User?.FullName)
                        ? "Dr. " + v.User.FullName
                        : (v.User?.UserName ?? "Doctor")
                }),
                "VeterinarianId",
                "DisplayName",
                vetId
            );

            var vetSlots = await _context.VetTimeSlots.ToListAsync();
            ViewBag.VetSlotsData = vets.Select(v => new
            {
                vetId = v.VeterinarianId,
                slots = vetSlots
                    .Where(s => s.VeterinarianId == v.VeterinarianId)
                    .Select(s => new
                    {
                        dayOfWeek = (int)s.DayOfWeek,
                        startTime = s.StartTime.ToString(@"hh\:mm"),
                        endTime = s.EndTime.ToString(@"hh\:mm")
                    }).ToList()
            }).ToList();

            ViewBag.VetSuggestData = vets.Select(v => new
            {
                id = v.VeterinarianId,
                name = v.User?.FullName ?? v.User?.UserName ?? "Doctor",
                specialization = v.Specialization ?? "",
                address = v.Address ?? "",
                experience = v.Experience
            }).ToList();

            ViewBag.OwnerAddress = currentUser?.Address ?? "";
            ViewBag.PetsInfo = sharedPets.Select(p => new { id = p.PetId, species = p.Species }).ToList();

            return View();
        }

        // POST: Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            var userId = _userManager.GetUserId(User);
            var familyUserIds = await GetFamilyUserIdsAsync(userId!);

            // The booked pet must belong to the booker or a family member.
            var petOk = await _context.Pets.AnyAsync(p => p.PetId == appointment.PetId && familyUserIds.Contains(p.OwnerId));
            if (!petOk)
            {
                return Forbid();
            }

            if (appointment.AppointmentDate < DateTime.Now)
            {
                ModelState.AddModelError("AppointmentDate", "You cannot book an appointment for a past date or time.");
            }

            ModelState.Remove("Pet");
            ModelState.Remove("Owner");
            ModelState.Remove("Veterinarian");
            ModelState.Remove("OwnerId");

            if (!ModelState.IsValid)
            {
                var sharedPets = await _context.Pets.Where(p => familyUserIds.Contains(p.OwnerId)).ToListAsync();
                var vets = await _context.Veterinarians.Include(v => v.User).ToListAsync();

                ViewBag.PetId = new SelectList(sharedPets, "PetId", "Name", appointment.PetId);
                ViewBag.VeterinarianId = new SelectList(vets, "VeterinarianId", "User.FullName", appointment.VeterinarianId);

                return View(appointment);
            }

            // The appointment is always booked under the actual logged-in
            // member's name, even if it's for a family member's pet - this is
            // what shows up as "Booked by" for everyone else in the family.
            appointment.OwnerId = userId!;
            appointment.Status = "Pending";

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Send notification to the Doctor
            // NOTE: previously this used _context.Veterinarians.FindAsync(...), which only
            // checks the PK. If the Veterinarian row's UserId didn't line up with a real
            // AspNetUsers row (e.g. a stale/duplicate profile), the notification insert
            // below could throw and silently abort AFTER the appointment was already saved -
            // the owner would see an error page while the booking had actually gone through,
            // and never know the doctor was never notified. Loading the User navigation
            // property up front lets us verify the link is actually valid before we try to
            // notify, and the whole block is now isolated so a failure here can never affect
            // the appointment that was already booked.
            var vet = await _context.Veterinarians.Include(v => v.User)
                .FirstOrDefaultAsync(v => v.VeterinarianId == appointment.VeterinarianId);
            var pet = await _context.Pets.FindAsync(appointment.PetId);
            var booker = await _context.Users.FindAsync(userId);

            if (vet?.User != null && pet != null)
            {
                try
                {
                    var dateText = appointment.AppointmentDate.ToString("dd MMM yyyy 'at' hh:mm tt");
                    var ownerName = booker?.FullName ?? booker?.UserName ?? "A pet owner";
                    _context.Notifications.Add(new AppNotification
                    {
                        UserId = vet.UserId,
                        Message = $"New Appointment Request: {ownerName} booked an appointment for {pet.Name} on {dateText}.",
                        Link = "/Veterinarian/Index",
                        DateCreated = DateTime.Now,
                        IsRead = false
                    });
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Appointment itself is already saved - don't fail the whole booking
                    // just because the notification insert had a problem.
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Appointment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var familyUserIds = await GetFamilyUserIdsAsync(userId!);

            var appointment = await _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.Owner)
                .Include(a => a.Veterinarian)
                .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(m => m.AppointmentId == id && familyUserIds.Contains(m.OwnerId));

            if (appointment == null)
            {
                return NotFound();
            }

            ViewBag.IsMine = appointment.OwnerId == userId;

            return View(appointment);
        }

        // POST: Appointment/ChangeStatus
        // Only the family member who actually booked it can cancel it - other
        // family members can see it but not touch it.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var userId = _userManager.GetUserId(User);

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == id && a.OwnerId == userId);

            if (appointment == null)
            {
                return NotFound();
            }

            if (status != "Cancelled")
            {
                return Forbid();
            }

            appointment.Status = status;

            var vet = await _context.Veterinarians.FindAsync(appointment.VeterinarianId);
            var pet = await _context.Pets.FindAsync(appointment.PetId);
            if (vet != null && pet != null && !string.IsNullOrEmpty(vet.UserId))
            {
                _context.Notifications.Add(new AppNotification
                {
                    UserId = vet.UserId,
                    Message = $"Appointment Cancelled: {pet.Name}'s appointment on {appointment.AppointmentDate:dd MMM yyyy} was cancelled by owner.",
                    Link = "/Veterinarian/Index",
                    DateCreated = DateTime.Now,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetVetSlots(int vetId)
        {
            var slots = await _context.VetTimeSlots
                .Where(s => s.VeterinarianId == vetId)
                .Select(s => new
                {
                    day = s.DayOfWeek.ToString(),
                    startTime = DateTime.Today.Add(s.StartTime).ToString("hh:mm tt"),
                    endTime = DateTime.Today.Add(s.EndTime).ToString("hh:mm tt")
                })
                .ToListAsync();

            return Json(slots);
        }
    }
}
