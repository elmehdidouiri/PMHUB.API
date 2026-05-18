using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using PMHUB.Shared.Helpers;

namespace PMHUB.Application.Services
{
    public class InternStatisticsService : IInternStatisticsService
    {
        private readonly IRepository<Intern> _internRepository;
        private readonly IRepository<InternAllocation> _internAllocationRepository;
        private readonly IRepository<InternHourEntry> _internHourEntryRepository;
        private readonly IRepository<User> _userRepository;
        private readonly ILogger<InternStatisticsService> _logger;

        public InternStatisticsService(
            IRepository<Intern> internRepository,
            IRepository<InternAllocation> internAllocationRepository,
            IRepository<InternHourEntry> internHourEntryRepository,
            IRepository<User> userRepository,
            ILogger<InternStatisticsService> logger)
        {
            _internRepository = internRepository;
            _internAllocationRepository = internAllocationRepository;
            _internHourEntryRepository = internHourEntryRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<InternStatisticsDto> GetInternStatisticsAsync(Guid internId)
        {
            _logger.LogInformation("GetInternStatisticsAsync - InternId: {InternId}", internId);

            var intern = await _internRepository.GetByIdAsync(internId)
                ?? throw new NotFoundException("Intern", internId);

            var allocations = (await _internAllocationRepository.FindAsync(ia => ia.InternId == internId)).ToList();

            var totalAllocated = allocations.Sum(ia => ia.AllocatedHours);
            var totalWorked = allocations.Sum(ia => ia.InternHourEntries.Sum(e => e.Hours));
            var utilizationRate = totalAllocated > 0 ? (totalWorked / totalAllocated) * 100m : 0m;

            var projectStats = allocations.Select(ia => new InternProjectAllocationStatsDto
            {
                AllocationId = ia.Id,
                ProjectId = ia.ProjectId,
                ProjectName = ia.Project?.Name ?? string.Empty,
                ProjectStatus = ia.Project?.Status.ToString() ?? string.Empty,
                ProjectPhase = ia.Project?.Phase.ToString() ?? string.Empty,
                AllocatedHours = ia.AllocatedHours,
                WorkedHours = ia.InternHourEntries.Sum(e => e.Hours),
                RemainingHours = Math.Max(0m, ia.AllocatedHours - ia.InternHourEntries.Sum(e => e.Hours)),
                HoursPercentage = ia.AllocatedHours > 0 ? (ia.InternHourEntries.Sum(e => e.Hours) / ia.AllocatedHours) * 100m : 0m,
                TotalHourEntries = ia.InternHourEntries.Count,
                AverageHoursPerEntry = ia.InternHourEntries.Count > 0 
                    ? ia.InternHourEntries.Sum(e => e.Hours) / ia.InternHourEntries.Count 
                    : 0m,
                AllocationDate = ia.AllocationDate,
                LastLoggedDate = ia.InternHourEntries.OrderByDescending(e => e.Date).FirstOrDefault()?.Date,
                Notes = ia.Notes
            }).ToList();

            return new InternStatisticsDto
            {
                InternId = intern.Id,
                InternName = intern.Name,
                RoleName = intern.Role?.Name ?? string.Empty,
                SupervisorName = intern.Supervisor is NormalUser su ? $"{su.FirstName} {su.LastName}".Trim() : string.Empty,
                SupervisorEmail = (intern.Supervisor as NormalUser)?.Email ?? string.Empty,
                TotalAllocatedHours = totalAllocated,
                TotalWorkedHours = totalWorked,
                TotalRemainingHours = Math.Max(0m, totalAllocated - totalWorked),
                UtilizationRate = Math.Round(utilizationRate, 2),
                TotalProjectAllocations = allocations.Count,
                ProjectAllocations = projectStats,
                CreatedAt = intern.CreatedAt,
                UpdatedAt = intern.UpdatedAt,
                IsActive = allocations.Any()
            };
        }

        public async Task<InternWorkVisualizationDto> GetInternWorkVisualizationAsync(Guid internId, DateTime? startDate = null, DateTime? endDate = null)
        {
            _logger.LogInformation("GetInternWorkVisualizationAsync - InternId: {InternId}, StartDate: {StartDate}, EndDate: {EndDate}", 
                internId, startDate, endDate);

            var intern = await _internRepository.GetByIdAsync(internId)
                ?? throw new NotFoundException("Intern", internId);

            var allocations = (await _internAllocationRepository.FindAsync(ia => ia.InternId == internId)).ToList();
            var hourEntries = allocations.SelectMany(ia => ia.InternHourEntries).ToList();

            // Filtrer par dates si fourni
            if (startDate.HasValue)
                hourEntries = hourEntries.Where(e => e.Date.Date >= startDate.Value.Date).ToList();
            if (endDate.HasValue)
                hourEntries = hourEntries.Where(e => e.Date.Date <= endDate.Value.Date).ToList();

            var actualStartDate = startDate ?? (hourEntries.Any() ? hourEntries.Min(e => e.Date) : DateTime.UtcNow.AddMonths(-1));
            var actualEndDate = endDate ?? (hourEntries.Any() ? hourEntries.Max(e => e.Date) : DateTime.UtcNow);

            var totalHours = hourEntries.Sum(e => e.Hours);
            var daysInRange = (actualEndDate.Date - actualStartDate.Date).Days + 1;
            var daysWithWork = hourEntries.Select(e => e.Date.Date).Distinct().Count();

            var projectBreakdowns = hourEntries
                .GroupBy(e => new { e.InternAllocation.ProjectId, e.InternAllocation.Project.Name })
                .Select(g => new InternWorkProjectBreakdownDto
                {
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.Name,
                    Hours = g.Sum(e => e.Hours),
                    Percentage = totalHours > 0 ? (g.Sum(e => e.Hours) / totalHours) * 100m : 0m,
                    EntryCount = g.Count()
                })
                .OrderByDescending(x => x.Hours)
                .ToList();

            var dailyWork = hourEntries
                .GroupBy(e => e.Date.Date)
                .OrderBy(g => g.Key)
                .Select(g => new InternDailyWorkDto
                {
                    Date = g.Key,
                    DayOfWeek = g.Key.DayOfWeek.ToString(),
                    TotalHours = g.Sum(e => e.Hours),
                    EntryCount = g.Count(),
                    ProjectBreakdown = g
                        .GroupBy(e => new { e.InternAllocation.ProjectId, e.InternAllocation.Project.Name })
                        .Select(pg => new InternDailyProjectBreakdownDto
                        {
                            ProjectId = pg.Key.ProjectId,
                            ProjectName = pg.Key.Name,
                            Hours = pg.Sum(e => e.Hours)
                        })
                        .ToList()
                })
                .ToList();

            return new InternWorkVisualizationDto
            {
                InternId = intern.Id,
                InternName = intern.Name,
                StartDate = actualStartDate,
                EndDate = actualEndDate,
                TotalDaysInRange = daysInRange,
                DaysWithWork = daysWithWork,
                TotalHours = Math.Round(totalHours, 2),
                AverageHoursPerDay = daysInRange > 0 ? Math.Round(totalHours / daysInRange, 2) : 0m,
                AverageHoursPerWorkDay = daysWithWork > 0 ? Math.Round(totalHours / daysWithWork, 2) : 0m,
                ProjectBreakdowns = projectBreakdowns,
                DailyWork = dailyWork
            };
        }

        public async Task<InternPeriodStatisticsDto> GetInternPeriodStatisticsAsync(Guid internId, int year, int? month = null)
        {
            _logger.LogInformation("GetInternPeriodStatisticsAsync - InternId: {InternId}, Year: {Year}, Month: {Month}", 
                internId, year, month);

            var intern = await _internRepository.GetByIdAsync(internId)
                ?? throw new NotFoundException("Intern", internId);

            var allocations = (await _internAllocationRepository.FindAsync(ia => ia.InternId == internId)).ToList();

            // Filtrer les entrées par période
            var startDate = month.HasValue
                ? new DateTime(month.Value >= 10 ? year - 1 : year, month.Value, 1)
                : CompanyYearHelper.GetCompanyYearStart(year);
            var endDate = month.HasValue
                ? startDate.AddMonths(1).AddTicks(-1)
                : CompanyYearHelper.GetCompanyYearEnd(year);

            var hourEntries = allocations.SelectMany(ia => ia.InternHourEntries)
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .ToList();

            var periodLabel = month.HasValue
                ? startDate.ToString("MMMM yyyy")
                : $"Année fiscale {year}";

            var totalHours = hourEntries.Sum(e => e.Hours);
            var allocatedInPeriod = allocations.Sum(ia => ia.AllocatedHours);
            var utilizationRate = allocatedInPeriod > 0 ? (totalHours / allocatedInPeriod) * 100m : 0m;

            var workDays = hourEntries.Select(e => e.Date.Date).Distinct().Count();
            var averageHours = workDays > 0 ? totalHours / workDays : 0m;

            var projectBreakdowns = hourEntries
                .GroupBy(e => new { e.InternAllocation.ProjectId, e.InternAllocation.Project.Name })
                .Select(g => new InternPeriodProjectStatsDto
                {
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.Name,
                    Hours = g.Sum(e => e.Hours),
                    Percentage = totalHours > 0 ? (g.Sum(e => e.Hours) / totalHours) * 100m : 0m
                })
                .OrderByDescending(x => x.Hours)
                .ToList();

            return new InternPeriodStatisticsDto
            {
                InternId = intern.Id,
                InternName = intern.Name,
                Year = year,
                Month = month,
                PeriodLabel = periodLabel,
                TotalHours = Math.Round(totalHours, 2),
                AllocatedHoursInPeriod = allocatedInPeriod,
                UtilizationRate = Math.Round(utilizationRate, 2),
                WorkDays = workDays,
                AverageHoursPerDay = Math.Round(averageHours, 2),
                ProjectBreakdowns = projectBreakdowns
            };
        }

        public async Task<InternsDashboardDto> GetInternsDashboardAsync()
        {
            _logger.LogInformation("GetInternsDashboardAsync - Fetching all interns statistics");

            var interns = (await _internRepository.GetAllAsync()).ToList();
            var allocations = (await _internAllocationRepository.FindAsync(ia => true)).ToList();

            var totalAllocated = allocations.Sum(ia => ia.AllocatedHours);
            var totalWorked = allocations.Sum(ia => ia.InternHourEntries.Sum(e => e.Hours));
            var utilizationRate = totalAllocated > 0 ? (totalWorked / totalAllocated) * 100m : 0m;

            var internsSummary = interns.Select(intern =>
            {
                var internAllocations = allocations.Where(ia => ia.InternId == intern.Id).ToList();
                var internWorked = internAllocations.Sum(ia => ia.InternHourEntries.Sum(e => e.Hours));
                var internAllocated = internAllocations.Sum(ia => ia.AllocatedHours);
                var internUtilization = internAllocated > 0 ? (internWorked / internAllocated) * 100m : 0m;

                return new InternSummaryStatsDto
                {
                    InternId = intern.Id,
                    InternName = intern.Name,
                    SupervisorName = intern.Supervisor is NormalUser su ? $"{su.FirstName} {su.LastName}".Trim() : string.Empty,
                    TotalAllocatedHours = internAllocated,
                    TotalWorkedHours = internWorked,
                    UtilizationRate = Math.Round(internUtilization, 2),
                    ProjectCount = internAllocations.Count,
                    HourEntryCount = internAllocations.Sum(ia => ia.InternHourEntries.Count)
                };
            }).ToList();

            var projectAllocations = allocations
                .GroupBy(ia => new { ia.ProjectId, ia.Project.Name })
                .Select(g => new ProjectInternAllocationSummaryDto
                {
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.Name,
                    InternCount = g.Select(x => x.InternId).Distinct().Count(),
                    TotalAllocatedHours = g.Sum(x => x.AllocatedHours),
                    TotalWorkedHours = g.Sum(x => x.InternHourEntries.Sum(e => e.Hours)),
                    UtilizationRate = g.Sum(x => x.AllocatedHours) > 0
                        ? (g.Sum(x => x.InternHourEntries.Sum(e => e.Hours)) / g.Sum(x => x.AllocatedHours)) * 100m
                        : 0m
                })
                .OrderByDescending(x => x.TotalAllocatedHours)
                .Take(10)
                .ToList();

            var activeInterns = internsSummary.Count(x => x.ProjectCount > 0);

            return new InternsDashboardDto
            {
                TotalInterns = interns.Count,
                ActiveInterns = activeInterns,
                InactiveInterns = interns.Count - activeInterns,
                TotalAllocatedHours = Math.Round(totalAllocated, 2),
                TotalWorkedHours = Math.Round(totalWorked, 2),
                OverallUtilizationRate = Math.Round(utilizationRate, 2),
                TotalProjectAllocations = allocations.Count,
                TotalHourEntries = allocations.Sum(ia => ia.InternHourEntries.Count),
                InternsSummary = internsSummary,
                TopProjectAllocations = projectAllocations
            };
        }

        public async Task<InternsDashboardDto> GetSupervisorInternsStatisticsAsync(Guid supervisorId)
        {
            _logger.LogInformation("GetSupervisorInternsStatisticsAsync - SupervisorId: {SupervisorId}", supervisorId);

            var supervisor = await _userRepository.GetByIdAsync(supervisorId)
                ?? throw new NotFoundException("User", supervisorId);

            var interns = (await _internRepository.FindAsync(i => i.SupervisorId == supervisorId)).ToList();
            var allocations = (await _internAllocationRepository.FindAsync(ia => true))
                .Where(ia => interns.Select(i => i.Id).Contains(ia.InternId))
                .ToList();

            var totalAllocated = allocations.Sum(ia => ia.AllocatedHours);
            var totalWorked = allocations.Sum(ia => ia.InternHourEntries.Sum(e => e.Hours));
            var utilizationRate = totalAllocated > 0 ? (totalWorked / totalAllocated) * 100m : 0m;

            var internsSummary = interns.Select(intern =>
            {
                var internAllocations = allocations.Where(ia => ia.InternId == intern.Id).ToList();
                var internWorked = internAllocations.Sum(ia => ia.InternHourEntries.Sum(e => e.Hours));
                var internAllocated = internAllocations.Sum(ia => ia.AllocatedHours);
                var internUtilization = internAllocated > 0 ? (internWorked / internAllocated) * 100m : 0m;

                return new InternSummaryStatsDto
                {
                    InternId = intern.Id,
                    InternName = intern.Name,
                    SupervisorName = $"{(intern.Supervisor as NormalUser)?.FirstName} {(intern.Supervisor as NormalUser)?.LastName}".Trim(),
                    TotalAllocatedHours = internAllocated,
                    TotalWorkedHours = internWorked,
                    UtilizationRate = Math.Round(internUtilization, 2),
                    ProjectCount = internAllocations.Count,
                    HourEntryCount = internAllocations.Sum(ia => ia.InternHourEntries.Count)
                };
            }).ToList();

            var activeInterns = internsSummary.Count(x => x.ProjectCount > 0);

            return new InternsDashboardDto
            {
                TotalInterns = interns.Count,
                ActiveInterns = activeInterns,
                InactiveInterns = interns.Count - activeInterns,
                TotalAllocatedHours = Math.Round(totalAllocated, 2),
                TotalWorkedHours = Math.Round(totalWorked, 2),
                OverallUtilizationRate = Math.Round(utilizationRate, 2),
                TotalProjectAllocations = allocations.Count,
                TotalHourEntries = allocations.Sum(ia => ia.InternHourEntries.Count),
                InternsSummary = internsSummary,
                TopProjectAllocations = new List<ProjectInternAllocationSummaryDto>()
            };
        }

        public async Task DeleteInternAllDataAsync(Guid internId, Guid currentUserId)
        {
            _logger.LogInformation("DeleteInternAllDataAsync - InternId: {InternId}, CurrentUserId: {CurrentUserId}", internId, currentUserId);

            var intern = await _internRepository.GetByIdAsync(internId)
                ?? throw new NotFoundException("Intern", internId);

            var currentUser = await _userRepository.GetByIdAsync(currentUserId)
                ?? throw new NotFoundException("User", currentUserId);

            // Vérifier les permissions
            if (currentUser is not Admin && (currentUser is not NormalUser || (intern.SupervisorId != currentUserId)))
                throw new ForbiddenException("Only admin or the supervisor can delete intern data.");

            // Supprimer toutes les allocations et entrées d'heures du stagiaire
            var allocations = (await _internAllocationRepository.FindAsync(ia => ia.InternId == internId)).ToList();

            foreach (var allocation in allocations)
            {
                var hourEntries = (await _internHourEntryRepository.FindAsync(e => e.InternAllocationId == allocation.Id)).ToList();
                foreach (var entry in hourEntries)
                {
                    _internHourEntryRepository.Remove(entry);
                }
                _internAllocationRepository.Remove(allocation);
            }

            await _internHourEntryRepository.SaveChangesAsync();
            await _internAllocationRepository.SaveChangesAsync();

            _logger.LogInformation("DeleteInternAllDataAsync completed - Deleted {AllocationCount} allocations and data", allocations.Count);
        }

        public async Task DeleteAllInternDataAsync(Guid currentUserId)
        {
            _logger.LogInformation("DeleteAllInternDataAsync - CurrentUserId: {CurrentUserId}", currentUserId);

            var currentUser = await _userRepository.GetByIdAsync(currentUserId)
                ?? throw new NotFoundException("User", currentUserId);

            if (currentUser is not Admin)
                throw new ForbiddenException("Only administrators can delete all intern data.");

            // Supprimer toutes les entrées d'heures d'abord
            var allHourEntries = (await _internHourEntryRepository.FindAsync(e => true)).ToList();
            foreach (var entry in allHourEntries)
            {
                _internHourEntryRepository.Remove(entry);
            }
            await _internHourEntryRepository.SaveChangesAsync();

            // Puis supprimer toutes les allocations
            var allAllocations = (await _internAllocationRepository.FindAsync(ia => true)).ToList();
            foreach (var allocation in allAllocations)
            {
                _internAllocationRepository.Remove(allocation);
            }
            await _internAllocationRepository.SaveChangesAsync();

            _logger.LogInformation("DeleteAllInternDataAsync completed - Deleted all intern data");
        }
    }
}