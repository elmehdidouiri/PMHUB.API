using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using PMHUB.Shared.Models;

namespace PMHUB.Application.Services
{
    public class TargetSettingsService : ITargetSettingsService
    {
        private static readonly IReadOnlyDictionary<string, decimal> DefaultKpiTargets =
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["OTD"] = 85m,
                ["Effectiveness"] = 85m,
                ["CSA"] = 85m,
                ["MonthlyWorkingHours"] = 161.5m
            };

        private readonly IRepository<CompanyTargetSettings> _companyTargetsRepository;
        private readonly IRepository<KpiTargetSetting> _kpiTargetsRepository;
        private readonly CompanyStandards _configuredStandards;
        private readonly ILogger<TargetSettingsService> _logger;

        public TargetSettingsService(
            IRepository<CompanyTargetSettings> companyTargetsRepository,
            IRepository<KpiTargetSetting> kpiTargetsRepository,
            IOptions<CompanyStandards> configuredStandards,
            ILogger<TargetSettingsService> logger)
        {
            _companyTargetsRepository = companyTargetsRepository;
            _kpiTargetsRepository = kpiTargetsRepository;
            _configuredStandards = configuredStandards.Value;
            _logger = logger;
        }

        public async Task<CompanyTargetSettingsDto> GetCompanyTargetsAsync()
        {
            var settings = await GetOrCreateCompanyTargetsAsync();
            return MapCompanyTargets(settings);
        }

        public async Task<CompanyStandards> GetCompanyStandardsAsync()
        {
            var settings = await GetOrCreateCompanyTargetsAsync();
            return new CompanyStandards
            {
                HoursPerDay = settings.HoursPerDay,
                AnnualHoursTarget = settings.AnnualHoursTarget,
                WorkingDaysPerMonth = settings.WorkingDaysPerMonth,
                FiscalYearStartMonth = settings.FiscalYearStartMonth
            };
        }

        public async Task<CompanyTargetSettingsDto> UpdateCompanyTargetsAsync(UpdateCompanyTargetSettingsDto dto)
        {
            var settings = await GetOrCreateCompanyTargetsAsync();

            settings.HoursPerDay = dto.HoursPerDay;
            settings.AnnualHoursTarget = dto.AnnualHoursTarget;
            settings.WorkingDaysPerMonth = dto.WorkingDaysPerMonth;
            settings.FiscalYearStartMonth = dto.FiscalYearStartMonth;
            settings.UpdatedAt = DateTime.UtcNow;

            _companyTargetsRepository.Update(settings);
            await _companyTargetsRepository.SaveChangesAsync();

            _logger.LogInformation("Company target settings updated");
            return MapCompanyTargets(settings);
        }

        public async Task<IEnumerable<KpiTargetSettingDto>> GetKpiTargetsAsync(bool includeInactive = false)
        {
            await EnsureDefaultKpiTargetsAsync();
            var targets = await _kpiTargetsRepository.GetAllAsync();

            return targets
                .Where(t => includeInactive || t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.Name)
                .Select(MapKpiTarget);
        }

        public async Task<IReadOnlyDictionary<string, decimal>> GetActiveKpiTargetValuesAsync()
        {
            await EnsureDefaultKpiTargetsAsync();
            var targets = await _kpiTargetsRepository.GetAllAsync();

            return targets
                .Where(t => t.IsActive && t.TargetValue > 0)
                .GroupBy(t => t.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt).First().TargetValue, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<KpiTargetSettingDto?> GetKpiTargetByIdAsync(Guid id)
        {
            var target = await _kpiTargetsRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("KpiTargetSetting", id);

            return MapKpiTarget(target);
        }

        public async Task<KpiTargetSettingDto> CreateKpiTargetAsync(CreateKpiTargetSettingDto dto)
        {
            var name = NormalizeName(dto.Name);
            var existing = await _kpiTargetsRepository.FindAsync(t => t.Name == name);
            if (existing.Any())
                throw new ConflictException("KpiTargetSetting", name);

            var target = new KpiTargetSetting
            {
                Name = name,
                TargetValue = dto.TargetValue,
                Description = dto.Description,
                IsActive = dto.IsActive,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            await _kpiTargetsRepository.AddAsync(target);
            await _kpiTargetsRepository.SaveChangesAsync();

            return MapKpiTarget(target);
        }

        public async Task<KpiTargetSettingDto> UpdateKpiTargetAsync(Guid id, UpdateKpiTargetSettingDto dto)
        {
            var target = await _kpiTargetsRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("KpiTargetSetting", id);

            var name = NormalizeName(dto.Name);
            var existing = await _kpiTargetsRepository.FindAsync(t => t.Name == name && t.Id != id);
            if (existing.Any())
                throw new ConflictException("KpiTargetSetting", name);

            target.Name = name;
            target.TargetValue = dto.TargetValue;
            target.Description = dto.Description;
            target.IsActive = dto.IsActive;
            target.DisplayOrder = dto.DisplayOrder;
            target.UpdatedAt = DateTime.UtcNow;

            _kpiTargetsRepository.Update(target);
            await _kpiTargetsRepository.SaveChangesAsync();

            return MapKpiTarget(target);
        }

        public async Task DeleteKpiTargetAsync(Guid id)
        {
            var target = await _kpiTargetsRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("KpiTargetSetting", id);

            _kpiTargetsRepository.Remove(target);
            await _kpiTargetsRepository.SaveChangesAsync();
        }

        private async Task<CompanyTargetSettings> GetOrCreateCompanyTargetsAsync()
        {
            var existing = (await _companyTargetsRepository.GetAllAsync()).FirstOrDefault();
            if (existing is not null)
                return existing;

            var settings = new CompanyTargetSettings
            {
                HoursPerDay = _configuredStandards.HoursPerDay,
                AnnualHoursTarget = _configuredStandards.AnnualHoursTarget,
                WorkingDaysPerMonth = _configuredStandards.WorkingDaysPerMonth,
                FiscalYearStartMonth = _configuredStandards.FiscalYearStartMonth,
                CreatedAt = DateTime.UtcNow
            };

            await _companyTargetsRepository.AddAsync(settings);
            await _companyTargetsRepository.SaveChangesAsync();

            return settings;
        }

        private async Task EnsureDefaultKpiTargetsAsync()
        {
            var existingTargets = (await _kpiTargetsRepository.GetAllAsync()).ToList();
            var existingNames = existingTargets.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var displayOrder = existingTargets.Count;

            foreach (var defaultTarget in DefaultKpiTargets)
            {
                if (existingNames.Contains(defaultTarget.Key))
                    continue;

                await _kpiTargetsRepository.AddAsync(new KpiTargetSetting
                {
                    Name = defaultTarget.Key,
                    TargetValue = defaultTarget.Value,
                    IsActive = true,
                    DisplayOrder = displayOrder++,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (displayOrder != existingTargets.Count)
                await _kpiTargetsRepository.SaveChangesAsync();
        }

        private static string NormalizeName(string name) => name.Trim();

        private static CompanyTargetSettingsDto MapCompanyTargets(CompanyTargetSettings settings)
        {
            var monthlyTarget = settings.AnnualHoursTarget > 0
                ? Math.Round(settings.AnnualHoursTarget / 12m, 2)
                : 0m;

            return new CompanyTargetSettingsDto
            {
                Id = settings.Id,
                HoursPerDay = settings.HoursPerDay,
                AnnualHoursTarget = settings.AnnualHoursTarget,
                WorkingDaysPerMonth = settings.WorkingDaysPerMonth,
                FiscalYearStartMonth = settings.FiscalYearStartMonth,
                MonthlyHoursTarget = monthlyTarget,
                CreatedAt = settings.CreatedAt,
                UpdatedAt = settings.UpdatedAt
            };
        }

        private static KpiTargetSettingDto MapKpiTarget(KpiTargetSetting target) => new()
        {
            Id = target.Id,
            Name = target.Name,
            TargetValue = target.TargetValue,
            Description = target.Description,
            IsActive = target.IsActive,
            DisplayOrder = target.DisplayOrder,
            CreatedAt = target.CreatedAt,
            UpdatedAt = target.UpdatedAt
        };
    }
}
