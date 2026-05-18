using Microsoft.Extensions.Options;
using PMHUB.Application.DTOs;
using PMHUB.Application.IServices;

namespace PMHUB.API.BackgroundServices
{
    public class WeeklyHourBookingReminderHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<HourBookingReminderSettings> _settings;
        private readonly ILogger<WeeklyHourBookingReminderHostedService> _logger;

        public WeeklyHourBookingReminderHostedService(
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<HourBookingReminderSettings> settings,
            ILogger<WeeklyHourBookingReminderHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = settings;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var options = _settings.CurrentValue;

                if (!options.IsEnabled)
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                    continue;
                }

                var delay = CalculateDelayUntilNextRun(options);
                _logger.LogInformation("Next weekly hour booking reminder scheduled in {Delay}", delay);
                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await SendRemindersAsync(stoppingToken);
            }
        }

        private async Task SendRemindersAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reminderService = scope.ServiceProvider.GetRequiredService<IHourBookingReminderService>();
                var result = await reminderService.SendWeeklyHourAllocationRemindersAsync(stoppingToken);

                _logger.LogInformation(
                    "Weekly hour booking reminders completed. Total: {TotalUsers}, Sent: {SentCount}, Failed: {FailedCount}",
                    result.TotalUsers,
                    result.SentCount,
                    result.FailedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weekly hour booking reminder job failed");
            }
        }

        private static TimeSpan CalculateDelayUntilNextRun(HourBookingReminderSettings settings)
        {
            if (!Enum.TryParse<DayOfWeek>(settings.WeeklyReminderDay, true, out var reminderDay))
            {
                reminderDay = DayOfWeek.Monday;
            }

            var now = DateTimeOffset.Now;
            var daysUntilRun = ((int)reminderDay - (int)now.DayOfWeek + 7) % 7;
            var nextRunDate = now.Date.AddDays(daysUntilRun).Add(settings.WeeklyReminderTime);

            if (nextRunDate <= now)
            {
                nextRunDate = nextRunDate.AddDays(7);
            }

            return nextRunDate - now;
        }
    }
}
