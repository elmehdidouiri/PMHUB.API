using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IRepositories;
using PMHUB.Application.IServices;
using PMHUB.Application.Mappings;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using PMHUB.Infrastructure.Repositories.Implementation;
using PMHUB.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMHUB.Application.Services.Implementation
{
    public class HourEntryService : IHourEntryService
    {
        private readonly IHourEntryRepository _hourEntryRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRepository<UserHourlyRate> _userHourlyRateRepository;
        private readonly IInternAllocationRepository _internAllocationRepository;
        private readonly ITargetSettingsService _targetSettingsService;
        private readonly IBookingActivityNotificationPublisher _bookingActivityNotificationPublisher;
        private readonly ILogger<HourEntryService> _logger;

        public HourEntryService(
            IHourEntryRepository hourEntryRepository,
            IProjectRepository projectRepository,
            IUserRepository userRepository,
            IRepository<UserHourlyRate> userHourlyRateRepository,
            IInternAllocationRepository internAllocationRepository,
            ITargetSettingsService targetSettingsService,
            IBookingActivityNotificationPublisher bookingActivityNotificationPublisher,
            ILogger<HourEntryService> logger)
        {
            _hourEntryRepository = hourEntryRepository;
            _projectRepository = projectRepository;
            _userRepository = userRepository;
            _userHourlyRateRepository = userHourlyRateRepository;
            _internAllocationRepository = internAllocationRepository;
            _targetSettingsService = targetSettingsService;
            _bookingActivityNotificationPublisher = bookingActivityNotificationPublisher;
            _logger = logger;
        }

        public async Task<HourEntryDto> CreateAsync(CreateHourEntryDto dto, Guid userId)
        {
            _logger.LogInformation("Début CreateAsync — User: {UserId}, Category: {Category}", userId, dto.Category);

            var user = await _userRepository.GetNormalUserByIdAsync(userId)
                ?? throw new NotFoundException("User", userId);

            var userRate = await GetActiveUserRateAsync(userId);
            var isPremiumSelected = dto.BookingType == Domain.Enums.BookingType.Premium;
            var isProjectWorkMode = dto.Category == Domain.Enums.CategoryWork.Project;

            var datesToProcess = new List<DateTime>();

            switch (dto.DateSelectionMode)
            {
                case DateSelectionMode.SingleDay:
                    if (dto.SelectedDates != null && dto.SelectedDates.Any())
                        datesToProcess.Add(dto.SelectedDates[0]);
                    break;
                case DateSelectionMode.MultipleDays:
                    if (dto.SelectedDates != null)
                        datesToProcess.AddRange(dto.SelectedDates);
                    break;
                case DateSelectionMode.WeekRange:
                    if (dto.RangeStartDate.HasValue && dto.RangeEndDate.HasValue)
                    {
                        for (var date = dto.RangeStartDate.Value.Date; date <= dto.RangeEndDate.Value.Date; date = date.AddDays(1))
                        {
                            // On skip le weekend si on veut (optionnel)
                            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                                datesToProcess.Add(date);
                        }
                    }
                    break;
            }

            if (!datesToProcess.Any())
                throw new BadRequestException("No dates selected for booking.");

            HourEntry? lastCreatedEntry = null;
            var createdEntries = new List<HourEntry>();
            Project? project = null;

            if (isProjectWorkMode)
            {
                if (!dto.ProjectId.HasValue || dto.ProjectId.Value == Guid.Empty)
                    throw new BadRequestException("Project is required for project work bookings.");

                project = await _projectRepository.GetByIdAsync(dto.ProjectId.Value)
                    ?? throw new NotFoundException("Project", dto.ProjectId.Value);

                var canBookProjectHours = await _projectRepository.FindAsync(p =>
                    p.Id == dto.ProjectId.Value &&
                    (p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId)));
                if (!canBookProjectHours.Any())
                    throw new ForbiddenException("You must be a project team member or the project manager to book hours on this project.");
            }
            else if (dto.ProjectId.HasValue && dto.ProjectId.Value != Guid.Empty)
            {
                throw new BadRequestException("Project must be empty for non-project activities.");
            }

            foreach (var date in datesToProcess)
            {
                // Unicité : un seul booking par projet par jour
                if (isProjectWorkMode)
                {
                   var existing = await _hourEntryRepository.FindAsync(h => h.UserId == userId && h.ProjectId == dto.ProjectId.Value && h.Date.Date == date.Date);
                   if (existing.Any())
                       continue; // On passe ou on throw ? L'utilisateur a dit "on peut pas booker 2 fois", donc on skip ou on notifie. Ici on skip pour le bulk.
                }
                else
                {
                   var existing = await _hourEntryRepository.FindAsync(h => h.UserId == userId && h.Category == dto.Category && h.Date.Date == date.Date);
                   if (existing.Any())
                       continue;
                }

                var hours = ResolveHours(dto);

                var hourEntry = new HourEntry
                {
                    UserId = userId,
                    ProjectId = isProjectWorkMode ? dto.ProjectId : null,
                    Category = dto.Category,
                    AllocationType = dto.AllocationFrequency ?? Domain.Enums.AllocationType.Daily,
                    ProjectType = Domain.Enums.ProjectType.NewProject, // TODO: Mapper vers la bonne valeur ou adapter l'entité
                    Date = date.Date,
                    ExecutionHours = hours.Execution,
                    SupervisionHours = hours.Supervision,
                    ProcessHours = hours.Process,
                    ManagementHours = hours.Management,
                    RAndDHours = hours.RAndD,
                    WorkshopHours = hours.Workshop,
                    OtherHours = hours.Other,
                    InternManagementHours = hours.InternManagement,
                    Notes = dto.ActivityNote ?? dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                // Add supervised interns relation
                if (dto.SupervisedInterns != null && dto.SupervisedInterns.Any())
                {
                    foreach (var internDto in dto.SupervisedInterns)
                    {
                        var allocation = await _internAllocationRepository.GetByIdAsync(internDto.InternAllocationId)
                            ?? throw new NotFoundException("InternAllocation", internDto.InternAllocationId);
                        
                        hourEntry.InternSupervisions.Add(new HourEntryInternSupervision
                        {
                            InternAllocationId = internDto.InternAllocationId,
                            Hours = internDto.Hours
                        });
                    }
                }

                hourEntry.Calculate(userRate, isPremiumSelected);

                await _hourEntryRepository.AddAsync(hourEntry);
                lastCreatedEntry = hourEntry;
                createdEntries.Add(hourEntry);
            }

            await _hourEntryRepository.SaveChangesAsync();

            if (lastCreatedEntry == null)
                 throw new BadRequestException("All selected dates already have bookings for this project.");

            if (project is not null)
            {
                await RecalculateProjectActualHoursAndProgressAsync(project.Id);
            }

            foreach (var createdEntry in createdEntries)
            {
                await PublishBookingActivityAsync(
                    BuildBookingActivityNotification(
                        createdEntry,
                        user,
                        project?.Name ?? createdEntry.Category.ToString(),
                        "created"),
                    "created");
            }

            return lastCreatedEntry.ToDto(project?.Name ?? lastCreatedEntry.Category.ToString(), $"{user.FirstName} {user.LastName}");
        }

        public async Task<HourEntryDto> UpdateAsync(Guid id, UpdateHourEntryDto dto, Guid userId)
        {
            _logger.LogInformation("Début UpdateAsync — HourEntryId: {HourEntryId}, User: {UserId}", id, userId);

            try
            {
                var entry = await _hourEntryRepository.GetByIdAsync(id)
                    ?? throw new NotFoundException("HourEntry", id);

                if (entry.UserId != userId)
            throw new ForbiddenException("You can only update your own bookings.");

                if (dto.Category.HasValue && dto.Category.Value != entry.Category)
                {
                    if (entry.Category == Domain.Enums.CategoryWork.Project || dto.Category.Value == Domain.Enums.CategoryWork.Project)
                        throw new BadRequestException("Project bookings cannot be converted to non-project activities.");

                    entry.Category = dto.Category.Value;
                }

                var hours = ResolveHours(dto, entry.Category);
                entry.ExecutionHours = hours.Execution;
                entry.SupervisionHours = hours.Supervision;
                entry.ProcessHours = hours.Process;
                entry.ManagementHours = hours.Management;
                entry.RAndDHours = hours.RAndD;
                entry.WorkshopHours = hours.Workshop;
                entry.OtherHours = hours.Other;
                entry.Notes = dto.ActivityNote ?? dto.Notes;

                var userRate = await GetActiveUserRateAsync(userId);
                var isPremiumSelected = dto.BookingType.HasValue
                    ? dto.BookingType.Value == Domain.Enums.BookingType.Premium
                    : entry.IsPremium;

                if (dto.BookingType.HasValue && entry.IsPremium != isPremiumSelected &&
                    entry.PremiumApprovalStatus != Domain.Enums.ApprovalStatus.Pending)
                {
            throw new BadRequestException("You cannot change the booking type after a premium decision has been made.");
                }

                entry.Calculate(userRate, isPremiumSelected);

                _hourEntryRepository.Update(entry);
                await _hourEntryRepository.SaveChangesAsync();
                if (entry.ProjectId.HasValue)
                    await RecalculateProjectActualHoursAndProgressAsync(entry.ProjectId.Value);

                var project = entry.ProjectId.HasValue
                    ? await _projectRepository.GetByIdAsync(entry.ProjectId.Value)
                    : null;
                var user = await _userRepository.GetNormalUserByIdAsync(userId);

                if (user is not null)
                {
                    await PublishBookingActivityAsync(
                        BuildBookingActivityNotification(
                            entry,
                            user,
                            project?.Name ?? entry.Category.ToString(),
                            "updated"),
                        "updated");
                }

                _logger.LogInformation("UpdateAsync terminé — HourEntryId: {HourEntryId}, TotalHours: {TotalHours}", entry.Id, entry.TotalHours);

                return entry.ToDto(project?.Name ?? entry.Category.ToString(), $"{user!.FirstName} {user.LastName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur UpdateAsync — HourEntryId: {HourEntryId}, User: {UserId}", id, userId);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id, Guid userId)
        {
            _logger.LogInformation("Début DeleteAsync — HourEntryId: {HourEntryId}, User: {UserId}", id, userId);

            try
            {
                var entry = await _hourEntryRepository.GetByIdAsync(id)
                    ?? throw new NotFoundException("HourEntry", id);

                if (entry.UserId != userId)
            throw new ForbiddenException("You can only delete your own bookings.");

                var projectId = entry.ProjectId;
                var project = projectId.HasValue
                    ? await _projectRepository.GetByIdAsync(projectId.Value)
                    : null;
                var user = await _userRepository.GetNormalUserByIdAsync(userId)
                    ?? throw new NotFoundException("User", userId);
                var notification = BuildBookingActivityNotification(
                    entry,
                    user,
                    project?.Name ?? entry.Category.ToString(),
                    "deleted");

                _hourEntryRepository.Remove(entry);
                await _hourEntryRepository.SaveChangesAsync();
                if (projectId.HasValue)
                    await RecalculateProjectActualHoursAndProgressAsync(projectId.Value);

                await PublishBookingActivityAsync(notification, "deleted");

                _logger.LogInformation("DeleteAsync terminé — HourEntryId: {HourEntryId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur DeleteAsync — HourEntryId: {HourEntryId}, User: {UserId}", id, userId);
                throw;
            }
        }

        public async Task<IEnumerable<HourEntrySummaryDto>> GetMyEntriesAsync(Guid userId)
        {
            _logger.LogInformation("GetMyEntriesAsync — User: {UserId}", userId);
            var entries = await _hourEntryRepository.FindWithIncludesAsync(h => h.UserId == userId);
            return entries.OrderByDescending(h => h.Date).ToSummaryDtoList();
        }

        public async Task<IEnumerable<HourEntrySummaryDto>> GetMyEntriesByDateAsync(Guid userId, DateTime date)
        {
            _logger.LogInformation("GetMyEntriesByDateAsync — User: {UserId}, Date: {Date}", userId, date);
            var entries = await _hourEntryRepository.FindWithIncludesAsync(h => h.UserId == userId && h.Date.Date == date.Date);
            return entries.ToSummaryDtoList();
        }

        public async Task<IEnumerable<HourEntrySummaryDto>> GetMyEntriesByMonthAsync(Guid userId, int year, int month)
        {
            _logger.LogInformation("GetMyEntriesByMonthAsync — User: {UserId}, Year: {Year}, Month: {Month}", userId, year, month);
            var entries = await _hourEntryRepository.FindWithIncludesAsync(h => h.UserId == userId && h.Date.Year == year && h.Date.Month == month);
            return entries.OrderByDescending(h => h.Date).ToSummaryDtoList();
        }

        public async Task<IEnumerable<HourEntrySummaryDto>> GetMyEntriesByProjectAsync(Guid userId, Guid projectId)
        {
            _logger.LogInformation("GetMyEntriesByProjectAsync — User: {UserId}, ProjectId: {ProjectId}", userId, projectId);
            var entries = await _hourEntryRepository.FindWithIncludesAsync(h => h.UserId == userId && h.ProjectId == projectId);
            return entries.OrderByDescending(h => h.Date).ToSummaryDtoList();
        }

        public async Task<IEnumerable<HourEntrySummaryDto>> GetByProjectAsync(Guid projectId)
        {
            _logger.LogInformation("GetByProjectAsync — ProjectId: {ProjectId}", projectId);
            var entries = await _hourEntryRepository.FindWithIncludesAsync(h => h.ProjectId == projectId);
            return entries.OrderByDescending(h => h.Date).ToSummaryDtoList();
        }

        public async Task<MonthlyHoursDashboardDto> GetMonthlyDashboardAsync(Guid userId, int year, int month)
        {
            _logger.LogInformation("GetMonthlyDashboardAsync — User: {UserId}, Year: {Year}, Month: {Month}", userId, year, month);
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var today = DateTime.Today;
            
            var standards = await _targetSettingsService.GetCompanyStandardsAsync();
            var workingDays = standards.WorkingDaysPerMonth;
            var targetHours = standards.MonthlyHoursTarget;

            var entries = await _hourEntryRepository.FindWithIncludesAsync(h => h.UserId == userId && h.Date >= startDate && h.Date <= endDate);
            var entriesList = entries.ToList();

            var loggedHours = entriesList.Sum(h => h.TotalHours);
            var totalCost = entriesList.Sum(h => h.TotalCost);

            var premiumHours = entriesList.Where(h => h.IsPremium && h.PremiumApprovalStatus == Domain.Enums.ApprovalStatus.Approved).Sum(h => h.TotalHours);
            var premiumPendingHours = entriesList.Where(h => h.IsPremium && h.PremiumApprovalStatus == Domain.Enums.ApprovalStatus.Pending).Sum(h => h.TotalHours);

            var totalExecutionHours = entriesList.Sum(h => h.ExecutionHours);
            var totalSupervisionHours = entriesList.Sum(h => h.SupervisionHours);
            var totalProcessHours = entriesList.Sum(h => h.ProcessHours);
            var totalManagementHours = entriesList.Sum(h => h.ManagementHours);
            var totalRAndDHours = entriesList.Sum(h => h.RAndDHours);
            var totalWorkshopHours = entriesList.Sum(h => h.WorkshopHours);
            var totalOtherHours = entriesList.Sum(h => h.OtherHours);
            var totalInternManagementHours = entriesList.Sum(h => h.InternManagementHours);

            var daysLeft = Math.Max((endDate - today).Days, 0);
            var hoursNeeded = Math.Max(targetHours - loggedHours, 0);

            return new MonthlyHoursDashboardDto
            {
                Year = year,
                Month = month,
                LoggedHours = loggedHours,
                TargetHours = targetHours,
                Variance = loggedHours - targetHours,
                TotalCost = totalCost,
                WorkingDays = workingDays,
                DailyTarget = Math.Round(targetHours / workingDays, 1),
                DaysLeft = daysLeft,
                DailyNeeded = daysLeft > 0 ? Math.Round(hoursNeeded / daysLeft, 1) : 0,
                Progress = targetHours > 0 ? Math.Round((loggedHours / targetHours) * 100, 1) : 0,
                PremiumHours = premiumHours,
                PremiumPendingHours = premiumPendingHours,
                TotalExecutionHours = totalExecutionHours,
                TotalSupervisionHours = totalSupervisionHours,
                TotalProcessHours = totalProcessHours,
                TotalManagementHours = totalManagementHours,
                TotalRAndDHours = totalRAndDHours,
                TotalWorkshopHours = totalWorkshopHours,
                TotalOtherHours = totalOtherHours,
                TotalInternManagementHours = totalInternManagementHours,
                Entries = entriesList
                    .OrderByDescending(h => h.Date)
                    .ToDtoList()
                    .ToList()
            };
        }

        public async Task<YtdDashboardDto> GetYtdDashboardAsync(Guid userId, int companyYear)
        {
            _logger.LogInformation("GetYtdDashboardAsync — User: {UserId}, CompanyYear: {CompanyYear}", userId, companyYear);
            var startDate = CompanyYearHelper.GetCompanyYearStart(companyYear);
            var fiscalYearEndDate = CompanyYearHelper.GetCompanyYearEnd(companyYear);
            var today = DateTime.UtcNow.Date;
            var endDate = (today > fiscalYearEndDate ? fiscalYearEndDate : today).Date.AddDays(1).AddTicks(-1);

            var entries = await _hourEntryRepository.FindWithIncludesAsync(h => h.UserId == userId && h.Date >= startDate && h.Date <= endDate);
            var entriesList = entries.ToList();

            var ytdHours = entriesList.Sum(h => h.TotalHours);
            var ytdCost = entriesList.Sum(h => h.TotalCost);

            var premiumHours = entriesList.Where(h => h.IsPremium && h.PremiumApprovalStatus == Domain.Enums.ApprovalStatus.Approved).Sum(h => h.TotalHours);
            var premiumApprovedCost = entriesList.Where(h => h.IsPremium && h.PremiumApprovalStatus == Domain.Enums.ApprovalStatus.Approved).Sum(h => h.TotalCost);
            var premiumPendingHours = entriesList.Where(h => h.IsPremium && h.PremiumApprovalStatus == Domain.Enums.ApprovalStatus.Pending).Sum(h => h.TotalHours);

            var monthlyBreakdown = new List<MonthlyHoursDashboardDto>();
            for (int month = 10; month <= 12; month++)
                monthlyBreakdown.Add(await GetMonthlyDashboardAsync(userId, companyYear - 1, month));
            for (int month = 1; month <= 9; month++)
                monthlyBreakdown.Add(await GetMonthlyDashboardAsync(userId, companyYear, month));

            return new YtdDashboardDto
            {
                CompanyYear = companyYear,
                YtdHours = ytdHours,
                ExpectedHours = 1190m,
                Variance = ytdHours - 1190m,
                ProjectedYearEnd = ytdHours * 12,
                MonthlyRecommendation = 170m,
                YtdCost = ytdCost,
                PremiumHours = premiumHours,
                PremiumApprovedCost = premiumApprovedCost,
                PremiumPendingHours = premiumPendingHours,
                MonthlyBreakdown = monthlyBreakdown
            };
        }

        public async Task<IEnumerable<HourEntryDto>> GetPendingPremiumAsync()
        {
            _logger.LogInformation("GetPendingPremiumAsync appelé");
            var entries = await _hourEntryRepository.FindWithIncludesAsync(h => h.IsPremium && h.PremiumApprovalStatus == Domain.Enums.ApprovalStatus.Pending);
            return entries.Select(h => h.ToDto(h.Project?.Name ?? h.Category.ToString(), h.User != null ? $"{h.User.FirstName} {h.User.LastName}" : "N/A"));
        }

        public async Task ApprovePremiumAsync(ApprovePremiumDto dto)
        {
            _logger.LogInformation("ApprovePremiumAsync — HourEntryId: {HourEntryId}, Decision: {Decision}", dto.HourEntryId, dto.IsApproved);
            try
            {
                var entry = await _hourEntryRepository.GetByIdAsync(dto.HourEntryId)
                    ?? throw new NotFoundException("HourEntry", dto.HourEntryId);

                if (!entry.IsPremium)
            throw new BadRequestException("This entry is not a premium booking.");

                if (entry.PremiumApprovalStatus != Domain.Enums.ApprovalStatus.Pending)
            throw new BadRequestException("This entry has already been processed.");

                var userRate = await GetActiveUserRateAsync(entry.UserId);
                entry.ApplyPremiumDecision(dto.IsApproved, userRate);

                _hourEntryRepository.Update(entry);
                await _hourEntryRepository.SaveChangesAsync();

                _logger.LogInformation("ApprovePremiumAsync terminé — HourEntryId: {HourEntryId}, Nouveau coût: {Cost} {Currency}", entry.Id, entry.TotalCost, entry.Currency);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur ApprovePremiumAsync — HourEntryId: {HourEntryId}", dto.HourEntryId);
                throw;
            }
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetMyProjectsAsync(Guid userId)
        {
            _logger.LogInformation("GetMyProjectsAsync — User: {UserId}", userId);
            var projects = await _projectRepository.FindSummariesAsync(p =>
                p.ProjectManagerId == userId || p.ProjectMembers.Any(m => m.UserId == userId));
            return projects.Select(project =>
            {
                var actualHours = project.HourEntries.Sum(entry => entry.TotalHours);
                project.ActualHours = actualHours;
                project.ProgressPercentage = CalculateProgressPercentage(actualHours, project.EstimatedHours);

                return ProjectMapper.ToSummaryDto(project);
            });
        }

        private static HourValues ResolveHours(CreateHourEntryDto dto)
        {
            if (dto.Category == Domain.Enums.CategoryWork.Project)
            {
                return new HourValues(
                    dto.ExecutionHours,
                    dto.TechnicalSupervisionHours,
                    dto.ProcessRelatedHours,
                    dto.ProjectManagementHours,
                    dto.ResearchAndDevHours,
                    dto.WorkshopHours,
                    dto.OtherActivitiesHours,
                    dto.InternManagementHours);
            }

            var totalHours = dto.TotalHours
                ?? dto.WorkshopHours
                + dto.OtherActivitiesHours
                + dto.ExecutionHours
                + dto.TechnicalSupervisionHours
                + dto.ProcessRelatedHours
                + dto.ProjectManagementHours
                + dto.ResearchAndDevHours
                + dto.InternManagementHours;

            if (totalHours <= 0)
                throw new BadRequestException("Total hours are required for non-project activities.");

            return dto.Category == Domain.Enums.CategoryWork.Workshop
                ? new HourValues(0, 0, 0, 0, 0, totalHours, 0, 0)
                : new HourValues(0, 0, 0, 0, 0, 0, totalHours, 0);
        }

        private static HourValues ResolveHours(UpdateHourEntryDto dto, Domain.Enums.CategoryWork category)
        {
            if (category == Domain.Enums.CategoryWork.Project)
            {
                return new HourValues(
                    dto.ExecutionHours,
                    dto.SupervisionHours,
                    dto.ProcessHours,
                    dto.ManagementHours,
                    dto.RAndDHours,
                    dto.WorkshopHours,
                    dto.OtherHours,
                    0);
            }

            var totalHours = dto.TotalHours
                ?? dto.WorkshopHours
                + dto.OtherHours
                + dto.ExecutionHours
                + dto.SupervisionHours
                + dto.ProcessHours
                + dto.ManagementHours
                + dto.RAndDHours;

            if (totalHours <= 0)
                throw new BadRequestException("Total hours are required for non-project activities.");

            return category == Domain.Enums.CategoryWork.Workshop
                ? new HourValues(0, 0, 0, 0, 0, totalHours, 0, 0)
                : new HourValues(0, 0, 0, 0, 0, 0, totalHours, 0);
        }

        private sealed record HourValues(
            decimal Execution,
            decimal Supervision,
            decimal Process,
            decimal Management,
            decimal RAndD,
            decimal Workshop,
            decimal Other,
            decimal InternManagement);

        private async Task<UserHourlyRate> GetActiveUserRateAsync(Guid userId)
        {
            _logger.LogInformation("GetActiveUserRateAsync — User: {UserId}", userId);
            var rates = await _userHourlyRateRepository.FindAsync(r => r.UserId == userId && r.IsActive);
            var activeRate = rates.Where(r => r.IsEffectiveOn(DateTime.UtcNow)).OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();

            if (activeRate == null)
            {
                _logger.LogWarning("Aucun tarif trouvé pour User {UserId}, création d'un tarif par défaut", userId);
                activeRate = new UserHourlyRate
                {
                    UserId = userId,
                    NormalRateAmount = 50m,
                    PremiumRateAmount = 75m,
                    Currency = "EUR",
                    EffectiveFrom = DateTime.UtcNow,
                    IsActive = true
                };
                await _userHourlyRateRepository.AddAsync(activeRate);
                await _userHourlyRateRepository.SaveChangesAsync();
            }

            return activeRate;
        }

        
        public async Task<IEnumerable<ProjectInternAllocationDto>> GetSupervisedInternsAsync(Guid userId)
        {
            _logger.LogInformation("GetSupervisedInternsAsync — User: {UserId}", userId);
            
            // Get all intern allocations where the intern's supervisor is the current user
            var allocations = await _internAllocationRepository.FindWithIncludesAsync(ia => ia.Intern.SupervisorId == userId);
            
            return allocations.Select(ProjectInternMapper.ToDto);
        }
        private async Task RecalculateProjectActualHoursAndProgressAsync(Guid projectId)
        {
            if (projectId == Guid.Empty)
            {
                return;
            }

            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
            {
                return;
            }

            var totalActualHours = await _hourEntryRepository.SumAsync(
                h => h.ProjectId == projectId,
                h => h.TotalHours);

            project.ActualHours = totalActualHours;
            project.ProgressPercentage = CalculateProgressPercentage(totalActualHours, project.EstimatedHours);
            project.UpdatedAt = DateTime.UtcNow;

            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();
        }

        private static int CalculateProgressPercentage(decimal actualHours, decimal estimatedHours)
        {
            if (estimatedHours <= 0)
            {
                return 0;
            }

            var percentage = Math.Round((actualHours / estimatedHours) * 100m, MidpointRounding.AwayFromZero);
            return (int)Math.Clamp(percentage, 0m, 100m);
        }

        private static BookingActivityNotificationDto BuildBookingActivityNotification(
            HourEntry entry,
            NormalUser user,
            string activityTarget,
            string eventType)
        {
            var occurredAtUtc = DateTime.UtcNow;
            var verb = eventType switch
            {
                "created" => "booked",
                "updated" => "updated",
                "deleted" => "deleted",
                _ => eventType
            };

            var bookingType = entry.IsPremium ? "Premium" : "Normal";
            var userFullName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(userFullName))
            {
                userFullName = user.Email;
            }

            return new BookingActivityNotificationDto
            {
                Id = $"BookingActivity:{eventType}:{entry.Id}:{occurredAtUtc:yyyyMMddHHmmssfff}",
                EventType = eventType,
                HourEntryId = entry.Id,
                UserId = entry.UserId,
                UserFullName = userFullName,
                UserEmail = user.Email,
                ProjectId = entry.ProjectId,
                ActivityTarget = activityTarget,
                Category = entry.Category,
                AllocationType = entry.AllocationType,
                BookingType = bookingType,
                BookingDate = entry.Date,
                OccurredAtUtc = occurredAtUtc,
                TotalHours = entry.TotalHours,
                ExecutionHours = entry.ExecutionHours,
                SupervisionHours = entry.SupervisionHours,
                ProcessHours = entry.ProcessHours,
                ManagementHours = entry.ManagementHours,
                RAndDHours = entry.RAndDHours,
                WorkshopHours = entry.WorkshopHours,
                OtherHours = entry.OtherHours,
                InternManagementHours = entry.InternManagementHours,
                Notes = entry.Notes,
                Message = $"{userFullName} {verb} {entry.TotalHours:0.##}h on {activityTarget} for {entry.Date:yyyy-MM-dd} ({bookingType}, {entry.AllocationType}).",
                ActionUrl = $"/users/{entry.UserId}?tab=hours&date={entry.Date:yyyy-MM-dd}"
            };
        }

        private async Task PublishBookingActivityAsync(
            BookingActivityNotificationDto notification,
            string eventType)
        {
            try
            {
                await _bookingActivityNotificationPublisher.PublishAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to publish realtime booking activity notification for HourEntry {HourEntryId} ({EventType})",
                    notification.HourEntryId,
                    eventType);
            }
        }
    }
}


