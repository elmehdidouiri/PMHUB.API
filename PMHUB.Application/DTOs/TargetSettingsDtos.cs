using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class CompanyTargetSettingsDto
    {
        public Guid Id { get; set; }
        public decimal HoursPerDay { get; set; }
        public decimal AnnualHoursTarget { get; set; }
        public int WorkingDaysPerMonth { get; set; }
        public int FiscalYearStartMonth { get; set; }
        public decimal MonthlyHoursTarget { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateCompanyTargetSettingsDto
    {
        [Range(0.01, double.MaxValue)]
        public decimal HoursPerDay { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal AnnualHoursTarget { get; set; }

        [Range(1, 31)]
        public int WorkingDaysPerMonth { get; set; }

        [Range(1, 12)]
        public int FiscalYearStartMonth { get; set; }
    }

    public class KpiTargetSettingDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal TargetValue { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateKpiTargetSettingDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal TargetValue { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
    }

    public class UpdateKpiTargetSettingDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal TargetValue { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
    }
}
