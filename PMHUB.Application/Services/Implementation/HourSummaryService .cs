using PMHUB.Application.DTOs;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMHUB.Application.Services
{
    public class HourSummaryService : IHourSummaryService
    {
        private readonly IHourEntryRepository _hourEntryRepository;

        public HourSummaryService(IHourEntryRepository hourEntryRepository)
        {
            _hourEntryRepository = hourEntryRepository;
        }

        public async Task<IEnumerable<MonthlyHoursDto>> GetMonthlySummary(int year)
        {
            var allEntries = await _hourEntryRepository.FindWithIncludesAsync(h =>
                h.Date.Year == year);

            var summary = allEntries
                .GroupBy(h => h.Date.Month)
                .Select(g => new MonthlyHoursDto
                {
                    Month = g.Key,
                    MonthName = new DateTime(year, g.Key, 1).ToString("MMMM"), // maintenant setter disponible
                    TotalHours = g.Sum(x => x.TotalHours),
                    PremiumHours = g.Sum(x => x.IsPremium ? x.TotalHours : 0),
                    TotalCost = g.Sum(x => x.TotalCost)
                })
                .OrderBy(m => m.Month)
                .ToList();

            return summary;
        }

        public async Task<IEnumerable<ProjectHoursDto>> GetProjectSummary(int year)
        {
            var allEntries = await _hourEntryRepository.FindWithIncludesAsync(h =>
                h.Date.Year == year);

            var summary = allEntries
                .GroupBy(h => h.ProjectId)
                .Select(g => new ProjectHoursDto
                {
                    ProjectId = g.Key,
                    ProjectName = g.First().Project.Name,
                    TotalHours = g.Sum(x => x.TotalHours),
                    PremiumHours = g.Sum(x => x.IsPremium ? x.TotalHours : 0),
                    TotalCost = g.Sum(x => x.TotalCost)
                })
                .OrderByDescending(p => p.TotalHours)
                .ToList();

            return summary;
        }

        public async Task<IEnumerable<UserHoursDto>> GetUserSummary(int year)
        {
            var allEntries = await _hourEntryRepository.FindWithIncludesAsync(h =>
                h.Date.Year == year);

            var summary = allEntries
                .GroupBy(h => h.UserId)
                .Select(g => new UserHoursDto
                {
                    UserId = g.Key,
                    UserName = $"{g.First().User.FirstName} {g.First().User.LastName}", // concat FirstName + LastName
                    TotalHours = g.Sum(x => x.TotalHours),
                    PremiumHours = g.Sum(x => x.IsPremium ? x.TotalHours : 0),
                    TotalCost = g.Sum(x => x.TotalCost)
                })
                .OrderByDescending(u => u.TotalHours)
                .ToList();

            return summary;
        }

        public async Task<IEnumerable<ProjectHoursDto>> GetTopProjects(int year, int topCount = 5)
        {
            var projects = await GetProjectSummary(year);
            return projects.Take(topCount).ToList();
        }

        public async Task<decimal> GetTotalHoursAsync(int year, int month, Guid? userId = null, Guid? projectId = null)
        {
            var entries = await _hourEntryRepository.FindWithIncludesAsync(h =>
                h.Date.Year == year &&
                h.Date.Month == month &&
                (userId == null || h.UserId == userId) &&
                (projectId == null || h.ProjectId == projectId));

            return entries.Sum(e => e.TotalHours);
        }

        public async Task<Dictionary<string, decimal>> GetBreakdownAsync(int year, int month, Guid? userId = null, Guid? projectId = null)
        {
            var entries = await _hourEntryRepository.FindWithIncludesAsync(h =>
                h.Date.Year == year &&
                h.Date.Month == month &&
                (userId == null || h.UserId == userId) &&
                (projectId == null || h.ProjectId == projectId));

            var breakdown = new Dictionary<string, decimal>
            {
                { "ExecutionHours", entries.Sum(e => e.ExecutionHours) },
                { "SupervisionHours", entries.Sum(e => e.SupervisionHours) },
                { "ProcessHours", entries.Sum(e => e.ProcessHours) },
                { "ManagementHours", entries.Sum(e => e.ManagementHours) },
                { "RAndDHours", entries.Sum(e => e.RAndDHours) },
                { "WorkshopHours", entries.Sum(e => e.WorkshopHours) },
                { "OtherHours", entries.Sum(e => e.OtherHours) },
                { "TotalHours", entries.Sum(e => e.TotalHours) },
                { "PremiumHours", entries.Sum(e => e.IsPremium ? e.TotalHours : 0) },
                { "TotalCost", entries.Sum(e => e.TotalCost) }
            };

            return breakdown;
        }
    }
}