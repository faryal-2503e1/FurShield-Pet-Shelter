using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    // Simple in-app notification inbox, shared by every role (Shelter, PetOwner, Vet).
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Notification Inbox View
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // Synchronously generate any due date reminders for this user's pets (and family pets) that haven't been notified yet
            var currentUser = await _context.Users.FindAsync(userId);
            var familyUserIds = new List<string> { userId! };
            if (currentUser?.FamilyAccountId != null)
            {
                familyUserIds = await _context.Users
                    .Where(u => u.FamilyAccountId == currentUser.FamilyAccountId)
                    .Select(u => u.Id)
                    .ToListAsync();
            }

            var cutoff = DateTime.Now.Date.AddDays(7);
            var dueRecords = await _context.HealthRecords
                .Include(h => h.Pet)
                .Where(h => h.Pet != null 
                            && familyUserIds.Contains(h.Pet.OwnerId)
                            && h.NextDueDate != null 
                            && !h.DueReminderSent 
                            && h.NextDueDate.Value.Date <= cutoff)
                .ToListAsync();

            if (dueRecords.Any())
            {
                foreach (var record in dueRecords)
                {
                    var isOverdue = record.NextDueDate!.Value.Date < DateTime.Now.Date;
                    var dateText = record.NextDueDate.Value.ToString("dd MMM yyyy");
                    var vaccineInfo = !string.IsNullOrWhiteSpace(record.Vaccination) ? $" ({record.Vaccination})" : "";

                    var message = isOverdue
                        ? $"Reminder: {record.Pet.Name}'s vaccination/follow-up{vaccineInfo} was due on {dateText}. Please schedule a vet visit soon."
                        : $"Upcoming Reminder: {record.Pet.Name}'s vaccination/follow-up{vaccineInfo} is scheduled for {dateText}.";

                    _context.Notifications.Add(new AppNotification
                    {
                        UserId = record.Pet.OwnerId,
                        Message = message,
                        Link = $"/HealthRecord/Details/{record.HealthRecordId}",
                        DateCreated = DateTime.Now,
                        IsRead = false
                    });

                    record.DueReminderSent = true;
                }

                _context.HealthRecords.UpdateRange(dueRecords);
                await _context.SaveChangesAsync();
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.DateCreated)
                .ToListAsync();

            // Mark everything as read once the user opens the inbox.
            var unread = notifications.Where(n => !n.IsRead).ToList();
            if (unread.Any())
            {
                foreach (var n in unread)
                {
                    n.IsRead = true;
                }
                _context.Notifications.UpdateRange(unread);
                await _context.SaveChangesAsync();
            }

            return View(notifications);
        }

        // 2. Reusable Helper: Real-time notification insert karne ke liye
        [NonAction]
        public async Task SendNotificationAsync(string receiverUserId, string message, string link = "")
        {
            if (string.IsNullOrEmpty(receiverUserId)) return;

            var notification = new AppNotification
            {
                UserId = receiverUserId,
                Message = message,
                Link = link,
                DateCreated = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // 3. Header/Navbar me Unread Badge count dikhane ke liye (Optional API Call)
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(new { count });
        }
    }
}