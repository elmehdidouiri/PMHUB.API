using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using PMHUB.Infrastructure.Repositories.Implementation;
using PMHUB.Shared.Models;

namespace PMHUB.Application.Services.Implementation
{
    public class HourBookingReminderService : IHourBookingReminderService
    {
        private readonly IUserRepository _userRepository;
        private readonly IHourEntryRepository _hourEntryRepository;
        private readonly IRepository<Intern> _internRepository;
        private readonly IRepository<InternAllocation> _internAllocationRepository;
        private readonly IRepository<InternHourEntry> _internHourEntryRepository;
        private readonly IEmailService _emailService;
        private readonly HourBookingReminderSettings _settings;
        private readonly CompanyStandards _companyStandards;
        private readonly ILogger<HourBookingReminderService> _logger;

        public HourBookingReminderService(
            IUserRepository userRepository,
            IHourEntryRepository hourEntryRepository,
            IRepository<Intern> internRepository,
            IRepository<InternAllocation> internAllocationRepository,
            IRepository<InternHourEntry> internHourEntryRepository,
            IEmailService emailService,
            IOptions<HourBookingReminderSettings> settings,
            IOptions<CompanyStandards> companyStandards,
            ILogger<HourBookingReminderService> logger)
        {
            _userRepository = userRepository;
            _hourEntryRepository = hourEntryRepository;
            _internRepository = internRepository;
            _internAllocationRepository = internAllocationRepository;
            _internHourEntryRepository = internHourEntryRepository;
            _emailService = emailService;
            _settings = settings.Value;
            _companyStandards = companyStandards.Value;
            _logger = logger;
        }

        public async Task<HourBookingReminderResultDto> SendWeeklyHourAllocationRemindersAsync(CancellationToken cancellationToken = default)
        {
            var users = (await _userRepository.GetActiveApprovedNormalUsersAsync()).ToList();
            var result = new HourBookingReminderResultDto { TotalUsers = users.Count };
            var today = DateTime.UtcNow.Date;
            var currentWeekStart = GetWeekStart(today);
            var weekStart = currentWeekStart.AddDays(-7);
            var weekEnd = currentWeekStart.AddDays(-1);
            var expectedWeeklyHours = _companyStandards.HoursPerDay * 5m;

            foreach (var user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var weeklyHours = await _hourEntryRepository.SumUserHoursAsync(user.Id, weekStart, weekEnd);

                    if (weeklyHours >= expectedWeeklyHours)
                    {
                        continue;
                    }

                    var missingHours = expectedWeeklyHours - weeklyHours;
                    await _emailService.SendHoursAllocationReminderAsync(
                        user.Email,
                        user.FirstName,
                        weeklyHours,
                        expectedWeeklyHours,
                        missingHours);

                    result.SentCount++;
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    _logger.LogError(ex, "Failed to send weekly hour allocation reminder to user {UserId}", user.Id);
                }
            }

            return result;
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.Date.AddDays(-daysSinceMonday);
        }

        public async Task<IEnumerable<AdminHourBookingNotificationDto>> GetUsersWithoutRecentBookingsAsync(CancellationToken cancellationToken = default)
        {
            var thresholdDays = Math.Max(_settings.NoBookingThresholdDays, 1);
            var thresholdDate = DateTime.UtcNow.Date.AddDays(-thresholdDays);
            var users = (await _userRepository.GetActiveApprovedNormalUsersAsync()).ToList();
            var userIdsWithRecentEntries = await _hourEntryRepository.GetUserIdsWithEntriesSinceAsync(thresholdDate);
            var notifications = new List<AdminHourBookingNotificationDto>();

            foreach (var user in users.Where(u => !userIdsWithRecentEntries.Contains(u.Id)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var lastBookingDate = await _hourEntryRepository.GetLastBookingDateAsync(user.Id);
                var daysWithoutBooking = lastBookingDate.HasValue
                    ? (DateTime.UtcNow.Date - lastBookingDate.Value.Date).Days
                    : (DateTime.UtcNow.Date - user.CreatedAt.Date).Days;

                notifications.Add(new AdminHourBookingNotificationDto
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    LastBookingDate = lastBookingDate,
                    DaysWithoutBooking = Math.Max(daysWithoutBooking, thresholdDays),
                    Message = $"{user.FirstName} {user.LastName} has not booked hours for at least {thresholdDays} days."
                });
            }

            var internNotifications = await GetInternsWithoutRecentBookingsAsync(thresholdDate, thresholdDays, cancellationToken);
            notifications.AddRange(internNotifications);

            return notifications
                .OrderByDescending(n => n.DaysWithoutBooking)
                .ThenBy(n => n.LastName)
                .ThenBy(n => n.FirstName);
        }

        public async Task<IEnumerable<AdminMonthlyTargetNotificationDto>> GetUsersBelowMonthlyTargetAsync(
            int? year = null,
            int? month = null,
            CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var resolvedYear = year ?? today.Year;
            var resolvedMonth = month is >= 1 and <= 12 ? month.Value : today.Month;
            var monthStart = new DateTime(resolvedYear, resolvedMonth, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var targetHours = _companyStandards.MonthlyHoursTarget;
            var users = (await _userRepository.GetActiveApprovedNormalUsersAsync()).ToList();
            var notifications = new List<AdminMonthlyTargetNotificationDto>();

            foreach (var user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bookedHours = await _hourEntryRepository.SumUserHoursAsync(user.Id, monthStart, monthEnd);
                if (bookedHours >= targetHours)
                {
                    continue;
                }

                var missingHours = targetHours - bookedHours;
                var completionRate = targetHours > 0
                    ? Math.Round(bookedHours / targetHours * 100m, 2)
                    : 0m;

                notifications.Add(new AdminMonthlyTargetNotificationDto
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Year = resolvedYear,
                    Month = resolvedMonth,
                    BookedHours = Math.Round(bookedHours, 2),
                    TargetHours = Math.Round(targetHours, 2),
                    MissingHours = Math.Round(missingHours, 2),
                    CompletionRate = completionRate,
                    Message = $"{user.FirstName} {user.LastName} has booked {bookedHours:0.##}h out of {targetHours:0.##}h for {resolvedMonth:00}/{resolvedYear}."
                });
            }

            return notifications
                .OrderByDescending(n => n.MissingHours)
                .ThenBy(n => n.LastName)
                .ThenBy(n => n.FirstName);
        }

        public async Task SendSupervisorReminderAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userRepository.GetNormalUserByIdAsync(userId);
            if (user is null)
            {
                await SendInternSupervisorReminderAsync(userId, cancellationToken);
                return;
            }

            if (IsInternRole(user.Role?.Name))
            {
                throw new BadRequestException("Reminder emails for interns must be sent to their supervisor.");
            }

            var thresholdDays = Math.Max(_settings.NoBookingThresholdDays, 1);
            var thresholdDate = DateTime.UtcNow.Date.AddDays(-thresholdDays);
            var userIdsWithRecentEntries = await _hourEntryRepository.GetUserIdsWithEntriesSinceAsync(thresholdDate);

            if (userIdsWithRecentEntries.Contains(user.Id))
            {
                throw new BadRequestException("This user has already booked hours recently.");
            }

            await _emailService.SendSupervisorVisitReminderAsync(user.Email, user.FirstName);
        }

        private async Task<IEnumerable<AdminHourBookingNotificationDto>> GetInternsWithoutRecentBookingsAsync(
            DateTime thresholdDate,
            int thresholdDays,
            CancellationToken cancellationToken)
        {
            var interns = (await _internRepository.GetAllAsync()).ToList();
            if (!interns.Any())
            {
                return Enumerable.Empty<AdminHourBookingNotificationDto>();
            }

            var allocations = (await _internAllocationRepository.GetAllAsync()).ToList();
            var allocationById = allocations.ToDictionary(a => a.Id);
            var allocationIdsByIntern = allocations
                .GroupBy(a => a.InternId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.Id).ToHashSet());

            var recentEntries = await _internHourEntryRepository.FindAsync(e => e.Date >= thresholdDate);
            var internIdsWithRecentEntries = recentEntries
                .Where(e => allocationById.ContainsKey(e.InternAllocationId))
                .Select(e => allocationById[e.InternAllocationId].InternId)
                .ToHashSet();

            var allEntries = (await _internHourEntryRepository.GetAllAsync()).ToList();
            var lastBookingByIntern = allEntries
                .Where(e => allocationById.ContainsKey(e.InternAllocationId))
                .GroupBy(e => allocationById[e.InternAllocationId].InternId)
                .ToDictionary(g => g.Key, g => (DateTime?)g.Max(e => e.Date));

            var supervisorIds = interns.Select(i => i.SupervisorId).Distinct().ToList();
            var supervisors = (await _userRepository.FindAsync(u => supervisorIds.Contains(u.Id)))
                .OfType<NormalUser>()
                .ToDictionary(u => u.Id);

            var notifications = new List<AdminHourBookingNotificationDto>();

            foreach (var intern in interns.Where(i => !internIdsWithRecentEntries.Contains(i.Id)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!allocationIdsByIntern.ContainsKey(intern.Id) ||
                    !supervisors.TryGetValue(intern.SupervisorId, out var supervisor))
                {
                    continue;
                }

                var lastBookingDate = lastBookingByIntern.GetValueOrDefault(intern.Id);
                var daysWithoutBooking = lastBookingDate.HasValue
                    ? (DateTime.UtcNow.Date - lastBookingDate.Value.Date).Days
                    : (DateTime.UtcNow.Date - intern.CreatedAt.Date).Days;
                var supervisorName = $"{supervisor.FirstName} {supervisor.LastName}".Trim();

                notifications.Add(new AdminHourBookingNotificationDto
                {
                    UserId = intern.Id,
                    TargetType = "Intern",
                    FirstName = intern.Name,
                    Email = supervisor.Email,
                    SupervisorId = supervisor.Id,
                    SupervisorName = supervisorName,
                    SupervisorEmail = supervisor.Email,
                    LastBookingDate = lastBookingDate,
                    DaysWithoutBooking = Math.Max(daysWithoutBooking, thresholdDays),
                    Message = $"{intern.Name} has no booked intern hours for at least {thresholdDays} days. Reminder will be sent to supervisor {supervisorName}."
                });
            }

            return notifications;
        }

        private async Task SendInternSupervisorReminderAsync(Guid internId, CancellationToken cancellationToken)
        {
            var intern = await _internRepository.GetByIdAsync(internId)
                ?? throw new NotFoundException("User or Intern", internId);

            var thresholdDays = Math.Max(_settings.NoBookingThresholdDays, 1);
            var thresholdDate = DateTime.UtcNow.Date.AddDays(-thresholdDays);
            var allocations = (await _internAllocationRepository.FindAsync(a => a.InternId == intern.Id)).ToList();
            var allocationIds = allocations.Select(a => a.Id).ToHashSet();

            if (!allocationIds.Any())
            {
                throw new BadRequestException("This intern has no project allocation.");
            }

            var recentEntries = await _internHourEntryRepository.FindAsync(e =>
                allocationIds.Contains(e.InternAllocationId) && e.Date >= thresholdDate);

            if (recentEntries.Any())
            {
                throw new BadRequestException("This intern already has booked hours recently.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var supervisor = await _userRepository.GetNormalUserByIdAsync(intern.SupervisorId)
                ?? throw new NotFoundException("Supervisor", intern.SupervisorId);

            var allEntries = await _internHourEntryRepository.FindAsync(e => allocationIds.Contains(e.InternAllocationId));
            var lastBookingDate = allEntries
                .OrderByDescending(e => e.Date)
                .Select(e => (DateTime?)e.Date)
                .FirstOrDefault();
            var daysWithoutBooking = lastBookingDate.HasValue
                ? (DateTime.UtcNow.Date - lastBookingDate.Value.Date).Days
                : (DateTime.UtcNow.Date - intern.CreatedAt.Date).Days;

            await _emailService.SendInternBookingReminderToSupervisorAsync(
                supervisor.Email,
                supervisor.FirstName,
                intern.Name,
                Math.Max(daysWithoutBooking, thresholdDays));
        }

        private static bool IsInternRole(string? roleName) =>
            string.Equals(roleName, "intern", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(roleName, "stagiaire", StringComparison.OrdinalIgnoreCase);
    }
}
