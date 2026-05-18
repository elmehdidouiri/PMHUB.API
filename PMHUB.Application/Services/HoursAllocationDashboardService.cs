using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.IRepositories;
using PMHUB.Application.IServices;

namespace PMHUB.Application.Services
{
    public class HoursAllocationDashboardService : IHoursAllocationDashboardService
    {
        private readonly IHoursAllocationDashboardRepository _repository;
        private readonly IEmailService _emailService;
        private readonly ILogger<HoursAllocationDashboardService> _logger;

        public HoursAllocationDashboardService(
            IHoursAllocationDashboardRepository repository,
            IEmailService emailService,
            ILogger<HoursAllocationDashboardService> logger)
        {
            _repository = repository;
            _emailService = emailService;
            _logger = logger;
        }

        public Task<HoursAllocationFiltersDto> GetFiltersAsync()
        {
            return _repository.GetFiltersAsync();
        }

        public Task<HoursAllocationDashboardDto> GetDashboardAsync(HoursAllocationDashboardQueryDto query)
        {
            Normalize(query);
            return _repository.GetDashboardAsync(query);
        }

        public async Task<HoursAllocationReminderResultDto> SendRemindersAsync(HoursAllocationReminderRequestDto request)
        {
            Normalize(request);
            var recipients = await _repository.GetReminderRecipientsAsync(request);
            var result = new HoursAllocationReminderResultDto();

            foreach (var recipient in recipients.Where(r => !string.IsNullOrWhiteSpace(r.Email)))
            {
                await _emailService.SendHoursAllocationReminderAsync(
                    recipient.Email,
                    recipient.FirstName,
                    recipient.TotalHours);

                result.Sent++;
                result.Recipients.Add(recipient.Email);
            }

            _logger.LogInformation(
                "Hours allocation reminders sent | count={Count} | filters={@Filters}",
                result.Sent,
                request);

            return result;
        }

        private static void Normalize(HoursAllocationDashboardQueryDto query)
        {
            if (query.Month is < 1 or > 12)
                query.Month = null;

            if (query.FromDate.HasValue)
                query.FromDate = query.FromDate.Value.Date;

            if (query.ToDate.HasValue)
                query.ToDate = query.ToDate.Value.Date;

            if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
                (query.FromDate, query.ToDate) = (query.ToDate, query.FromDate);

            query.QuickSelect = query.QuickSelect?.Trim();
            query.Analysis = query.Analysis?.Trim();
        }
    }
}
