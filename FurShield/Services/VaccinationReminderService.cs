using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Services
{
    // SRS 1.6 "Notifications" -> vaccination due date reminders.
    //
    // Runs in the background for the lifetime of the app. It periodically checks
    // HealthRecords for a "Next Vaccination/Follow-up Due Date"
    // (HealthRecord.NextDueDate) that falls within the next 7 days (or is
    // already overdue) and hasn't been notified yet (DueReminderSent == false).
    // For each one it drops an in-app AppNotification for the pet's owner and
    // flags DueReminderSent so the same visit doesn't notify twice.
    public class VaccinationReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VaccinationReminderService> _logger;

        // How many days ahead of the due date the owner should start being reminded.
        private const int ReminderWindowDays = 7;

        // How often the check runs while the app is up (every 1 hour).
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

        public VaccinationReminderService(IServiceScopeFactory scopeFactory, ILogger<VaccinationReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Initial short wait (5 seconds) after app starts, then run immediately
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendDueRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // Never let a bad run crash the whole background service.
                    _logger.LogError(ex, "VaccinationReminderService: failed while sending due-date reminders.");
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // App is shutting down.
                    break;
                }
            }
        }

        public async Task<int> SendDueRemindersAsync(CancellationToken stoppingToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PetShelterContext>();

            var cutoff = DateTime.Now.Date.AddDays(ReminderWindowDays);

            var dueRecords = await context.HealthRecords
                .Include(h => h.Pet)
                .Where(h => h.NextDueDate != null
                            && !h.DueReminderSent
                            && h.NextDueDate.Value.Date <= cutoff)
                .ToListAsync(stoppingToken);

            if (!dueRecords.Any()) return 0;

            foreach (var record in dueRecords)
            {
                if (record.Pet == null || string.IsNullOrEmpty(record.Pet.OwnerId)) continue;

                var isOverdue = record.NextDueDate!.Value.Date < DateTime.Now.Date;
                var dateText = record.NextDueDate.Value.ToString("dd MMM yyyy");

                var vaccineInfo = !string.IsNullOrWhiteSpace(record.Vaccination)
                    ? $" ({record.Vaccination})"
                    : "";

                var message = isOverdue
                    ? $"Reminder: {record.Pet.Name}'s vaccination/follow-up{vaccineInfo} was due on {dateText}. Please schedule a vet visit soon."
                    : $"Upcoming Reminder: {record.Pet.Name}'s vaccination/follow-up{vaccineInfo} is scheduled for {dateText}.";

                context.Notifications.Add(new AppNotification
                {
                    UserId = record.Pet.OwnerId,
                    Message = message,
                    Link = $"/HealthRecord/Details/{record.HealthRecordId}",
                    DateCreated = DateTime.Now,
                    IsRead = false
                });

                record.DueReminderSent = true;
            }

            context.HealthRecords.UpdateRange(dueRecords);
            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation("VaccinationReminderService: sent {Count} due-date reminder(s).", dueRecords.Count);
            return dueRecords.Count;
        }
    }
}
