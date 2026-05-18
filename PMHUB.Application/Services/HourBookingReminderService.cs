using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Infrastructure.Repositories;
using PMHUB.Infrastructure.Repositories.Implementation;
using PMHUB.Shared.Models;

namespace PMHUB.Application.Services.Implementation
{
    public class HourBookingReminderService : IHourBookingReminderService
    {
        private readonly IUserRepository _userRepository;
        private readonly IHourEntryRepository _hourEntryRepository;
        private readonly IEmailService _emailService;
        private readonly HourBookingReminderSettings _settings;
        private readonly CompanyStandards _companyStandards;
        private readonly ILogger<HourBookingReminderService> _logger;

        public HourBookingReminderService(
            IUserRepository userRepository,
            IHourEntryRepository hourEntryRepository,
            IEmailService emailService,
            IOptions<HourBookingReminderSettings> settings,
            IOptions<CompanyStandards> companyStandards,
            ILogger<HourBookingReminderService> logger)
        {
            _userRepository = userRepository;
            _hourEntryRepository = hourEntryRepository;
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

            return notifications
                .OrderByDescending(n => n.DaysWithoutBooking)
                .ThenBy(n => n.LastName)
                .ThenBy(n => n.FirstName);
        }

        public async Task SendSupervisorReminderAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userRepository.GetNormalUserByIdAsync(userId)
                ?? throw new NotFoundException("User", userId);

            var thresholdDays = Math.Max(_settings.NoBookingThresholdDays, 1);
            var thresholdDate = DateTime.UtcNow.Date.AddDays(-thresholdDays);
            var userIdsWithRecentEntries = await _hourEntryRepository.GetUserIdsWithEntriesSinceAsync(thresholdDate);

            if (userIdsWithRecentEntries.Contains(user.Id))
            {
                throw new BadRequestException("This user has already booked hours recently.");
            }

            await _emailService.SendSupervisorVisitReminderAsync(user.Email, user.FirstName);
        }
    }
}
