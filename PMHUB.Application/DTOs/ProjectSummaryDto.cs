using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class ProjectSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public Category ProjectManagementType { get; set; }
        public string ProjectManagementTypeLabel => ProjectManagementType.ToString();
        public string ProjectType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? EstimatedStartDate { get; set; }
        public DateTime? EstimatedDueDate { get; set; }
        public int ProgressPercentage { get; set; }
        public int Progress => ProgressPercentage;
        public bool IsDelayed { get; set; }
        public decimal Budget { get; set; }
        public ICollection<Guid> DepartmentIds { get; set; } = new List<Guid>();
        public string DepartmentName { get; set; } = string.Empty;
        public ICollection<string> DepartmentNames { get; set; } = new List<string>();
        public string PlantName { get; set; } = string.Empty;
        public string Sponsor { get; set; } = string.Empty;
        public string CostCenter { get; set; } = string.Empty;
        public decimal EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }
        public decimal TotalBookingHoursCurrentMonth { get; set; }
        public decimal TotalBookingHoursFiscalYtd { get; set; }
        public ICollection<ProjectMonthlyBookingHoursDto> FiscalYtdMonthlyBookingHours { get; set; } = new List<ProjectMonthlyBookingHoursDto>();
        public bool IsDataComplete { get; set; }
        public int DataCompletionPercentage { get; set; }
        public ICollection<string> MissingFields { get; set; } = new List<string>();
    }

    public class ProjectMonthlyBookingHoursDto
    {
        public DateTime MonthStart { get; set; }
        public decimal Hours { get; set; }
    }
}
