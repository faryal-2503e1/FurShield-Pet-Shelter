using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    [Authorize(Roles = "Veterinarian")]
    public class VeterinarianController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VeterinarianController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Veterinarian/Index
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var vet = await _context.Veterinarians
                .FirstOrDefaultAsync(v => v.UserId == userId);

            // Profile na milne par auto-create kar dein
            if (vet == null)
            {
                vet = new Veterinarian
                {
                    UserId = userId!,
                    Specialization = "General Vet",
                    Experience = 1
                };
                _context.Veterinarians.Add(vet);
                await _context.SaveChangesAsync();
            }

            var appointments = await _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.Owner)
                .Where(a => a.VeterinarianId == vet.VeterinarianId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var vet = await _context.Veterinarians
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.UserId == currentUser.Id);

            if (vet == null)
            {
                vet = new Veterinarian { UserId = currentUser.Id, User = currentUser };
            }

            return View(vet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Veterinarian model, string FullName, string Email, string PhoneNumber)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            currentUser.FullName = FullName;
            currentUser.PhoneNumber = PhoneNumber;

            if (!string.IsNullOrEmpty(Email) && currentUser.Email != Email)
            {
                currentUser.Email = Email;
                currentUser.UserName = Email;
            }

            var userUpdateResult = await _userManager.UpdateAsync(currentUser);
            if (!userUpdateResult.Succeeded)
            {
                foreach (var error in userUpdateResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            var vet = await _context.Veterinarians.FirstOrDefaultAsync(v => v.UserId == currentUser.Id);
            if (vet != null)
            {
                vet.Specialization = model.Specialization;
                vet.Experience = model.Experience;
                vet.Address = model.Address;
                vet.Bio = model.Bio;

                _context.Veterinarians.Update(vet);
            }
            else
            {
                model.UserId = currentUser.Id;
                _context.Veterinarians.Add(model);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        // GET: Veterinarian/AddHealthRecord?appointmentId=5
        public async Task<IActionResult> AddHealthRecord(int appointmentId)
        {
            var userId = _userManager.GetUserId(User);

            var appointment = await _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.Veterinarian)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.Veterinarian.UserId == userId);

            if (appointment == null) return NotFound();

            ViewBag.PetName = appointment.Pet.Name;
            ViewBag.AppointmentId = appointment.AppointmentId;

            return View();
        }

        // POST: Veterinarian/AddHealthRecord
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHealthRecord(int appointmentId, HealthRecord record)
        {
            var userId = _userManager.GetUserId(User);

            var appointment = await _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.Veterinarian)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.Veterinarian.UserId == userId);

            if (appointment == null) return NotFound();

            ModelState.Remove("PetId");
            ModelState.Remove("VeterinarianId");
            ModelState.Remove("Pet");
            ModelState.Remove("Veterinarian");

            if (ModelState.IsValid)
            {
                record.PetId = appointment.PetId;
                record.VeterinarianId = appointment.VeterinarianId;
                record.VisitDate = DateTime.Now;

                // NextDueDate notification trigger
                if (record.NextDueDate.HasValue && record.NextDueDate.Value.Date <= DateTime.Now.Date.AddDays(7))
                {
                    record.DueReminderSent = true;
                    var isOverdue = record.NextDueDate.Value.Date < DateTime.Now.Date;
                    var dateText = record.NextDueDate.Value.ToString("dd MMM yyyy");
                    var vaccineInfo = !string.IsNullOrWhiteSpace(record.Vaccination) ? $" ({record.Vaccination})" : "";
                    var msg = isOverdue
                        ? $"Reminder from Vet: {appointment.Pet.Name}'s vaccination/follow-up{vaccineInfo} was due on {dateText}. Please schedule a visit."
                        : $"Upcoming Vet Reminder: Dr. scheduled {appointment.Pet.Name}'s next vaccination/follow-up{vaccineInfo} for {dateText}.";

                    _context.Notifications.Add(new AppNotification
                    {
                        UserId = appointment.Pet.OwnerId,
                        Message = msg,
                        Link = "/HealthRecord/Index",
                        DateCreated = DateTime.Now,
                        IsRead = false
                    });
                }
                else
                {
                    record.DueReminderSent = false;
                }

                _context.HealthRecords.Add(record);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PetName = appointment.Pet.Name;
            ViewBag.AppointmentId = appointmentId;
            return View(record);
        }

        // GET: Veterinarian/Availability - manage weekly time slots
        [HttpGet]
        public async Task<IActionResult> Availability()
        {
            var userId = _userManager.GetUserId(User);
            var vet = await _context.Veterinarians.FirstOrDefaultAsync(v => v.UserId == userId);
            if (vet == null) return NotFound();

            var slots = await _context.VetTimeSlots
                .Where(s => s.VeterinarianId == vet.VeterinarianId)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            return View(slots);
        }

        // POST: Veterinarian/AddTimeSlot
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTimeSlot(DayOfWeek dayOfWeek, string startTime, string endTime)
        {
            var userId = _userManager.GetUserId(User);
            var vet = await _context.Veterinarians.FirstOrDefaultAsync(v => v.UserId == userId);
            if (vet == null) return NotFound();

            if (TimeSpan.TryParse(startTime, out var start) && TimeSpan.TryParse(endTime, out var end) && end > start)
            {
                _context.VetTimeSlots.Add(new VetTimeSlot
                {
                    VeterinarianId = vet.VeterinarianId,
                    DayOfWeek = dayOfWeek,
                    StartTime = start,
                    EndTime = end
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Time slot added.";
            }
            else
            {
                TempData["ErrorMessage"] = "End time must be after start time.";
            }

            return RedirectToAction(nameof(Availability));
        }

        // POST: Veterinarian/DeleteTimeSlot
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTimeSlot(int id)
        {
            var userId = _userManager.GetUserId(User);

            var slot = await _context.VetTimeSlots
                .Include(s => s.Veterinarian)
                .FirstOrDefaultAsync(s => s.VetTimeSlotId == id && s.Veterinarian.UserId == userId);

            if (slot != null)
            {
                _context.VetTimeSlots.Remove(slot);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Availability));
        }

        // POST: Veterinarian/Reschedule - vet proposes a new date/time for an appointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int appointmentId, DateTime newDate)
        {
            var userId = _userManager.GetUserId(User);

            if (newDate < DateTime.Now)
            {
                TempData["ErrorMessage"] = "You cannot reschedule an appointment to a past date or time.";
                return RedirectToAction(nameof(Index));
            }

            var appointment = await _context.Appointments
                .Include(a => a.Veterinarian)
                    .ThenInclude(v => v.User)
                .Include(a => a.Pet)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.Veterinarian.UserId == userId);

            if (appointment == null) return NotFound();

            appointment.AppointmentDate = newDate;
            appointment.Status = "Rescheduled";

            _context.Appointments.Update(appointment);

            _context.Notifications.Add(new AppNotification
            {
                UserId = appointment.OwnerId,
                Message = $"Your appointment for {appointment.Pet.Name} with Dr. {appointment.Veterinarian.User.FullName} was rescheduled to {newDate:dddd, dd MMM yyyy 'at' hh:mm tt}.",
                Link = "/Appointment/Index",
                DateCreated = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Appointment rescheduled successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Veterinarian/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int appointmentId, string status)
        {
            var userId = _userManager.GetUserId(User);

            var appointment = await _context.Appointments
                .Include(a => a.Veterinarian)
                    .ThenInclude(v => v.User)
                .Include(a => a.Pet)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.Veterinarian.UserId == userId);

            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = status;
            _context.Appointments.Update(appointment);

            string notifyMessage = status switch
            {
                "Confirmed" => $"Your appointment for {appointment.Pet.Name} with Dr. {appointment.Veterinarian.User.FullName} has been confirmed.",
                "Completed" => $"Your appointment for {appointment.Pet.Name} with Dr. {appointment.Veterinarian.User.FullName} has been marked as completed.",
                "Cancelled" => $"Your appointment for {appointment.Pet.Name} with Dr. {appointment.Veterinarian.User.FullName} was cancelled by the vet.",
                _ => $"Your appointment for {appointment.Pet.Name} with Dr. {appointment.Veterinarian.User.FullName} was updated to '{status}'."
            };

            _context.Notifications.Add(new AppNotification
            {
                UserId = appointment.OwnerId,
                Message = notifyMessage,
                Link = "/Appointment/Index",
                DateCreated = DateTime.Now
            });

            // Self-reminder for the doctor: if this appointment was just marked
            // Completed but no health record has been logged yet for this pet by
            // this vet (on or after the appointment date), nudge them so it
            // doesn't get forgotten. HealthRecord has no direct AppointmentId link,
            // so we match by Pet + Vet + date as the closest reasonable check.
            if (status == "Completed")
            {
                var hasRecord = await _context.HealthRecords.AnyAsync(h =>
                    h.PetId == appointment.PetId &&
                    h.VeterinarianId == appointment.VeterinarianId &&
                    h.VisitDate.Date >= appointment.AppointmentDate.Date);

                if (!hasRecord)
                {
                    _context.Notifications.Add(new AppNotification
                    {
                        UserId = userId!,
                        Message = $"Reminder: You marked {appointment.Pet.Name}'s appointment as completed but haven't added a health record for this visit yet.",
                        Link = $"/Veterinarian/AddHealthRecord?appointmentId={appointment.AppointmentId}",
                        DateCreated = DateTime.Now,
                        IsRead = false
                    });
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // GET: Veterinarian/PetHistory or Veterinarian/PetHistory?petId=5
        [HttpGet]
        public async Task<IActionResult> PetHistory(int? petId)
        {
            var userId = _userManager.GetUserId(User);

            if (petId == null)
            {
                var allRecords = await _context.HealthRecords
                    .Include(h => h.Pet)
                    .Include(h => h.Veterinarian)
                    .ThenInclude(v => v.User)
                    .Where(h => h.Veterinarian.UserId == userId)
                    .OrderByDescending(h => h.VisitDate)
                    .ToListAsync();

                ViewBag.PetName = "All Patients";
                ViewBag.PetDocuments = new List<PetDocument>(); // Correct Class Name
                return View(allRecords);
            }

            var pet = await _context.Pets
                .FirstOrDefaultAsync(p => p.PetId == petId);

            if (pet == null) return NotFound();

            // 1. Fetch Health Records for this Pet
            var records = await _context.HealthRecords
                .Include(h => h.Pet)
                .Include(h => h.Veterinarian)
                .ThenInclude(v => v.User)
                .Where(h => h.PetId == petId)
                .OrderByDescending(h => h.VisitDate)
                .ToListAsync();

            // 2. FETCH DOCUMENTS (Pet Owner Uploaded Reports/Certificates)
            var documents = await _context.PetDocuments
                .Where(d => d.PetId == petId)
                .OrderByDescending(d => d.UploadedDate)
                .ToListAsync();

            ViewBag.PetName = pet.Name;
            ViewBag.PetDocuments = documents; // Pass documents to the View

            return View(records);
        }
       
    }
}