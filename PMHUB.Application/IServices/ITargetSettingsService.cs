using PMHUB.Application.DTOs;
using PMHUB.Shared.Models;

namespace PMHUB.Application.IServices
{
    public interface ITargetSettingsService
    {
        Task<CompanyTargetSettingsDto> GetCompanyTargetsAsync();
        Task<CompanyStandards> GetCompanyStandardsAsync();
        Task<CompanyTargetSettingsDto> UpdateCompanyTargetsAsync(UpdateCompanyTargetSettingsDto dto);
        Task<IEnumerable<KpiTargetSettingDto>> GetKpiTargetsAsync(bool includeInactive = false);
        Task<IReadOnlyDictionary<string, decimal>> GetActiveKpiTargetValuesAsync();
        Task<KpiTargetSettingDto?> GetKpiTargetByIdAsync(Guid id);
        Task<KpiTargetSettingDto> CreateKpiTargetAsync(CreateKpiTargetSettingDto dto);
        Task<KpiTargetSettingDto> UpdateKpiTargetAsync(Guid id, UpdateKpiTargetSettingDto dto);
        Task DeleteKpiTargetAsync(Guid id);
    }
}
