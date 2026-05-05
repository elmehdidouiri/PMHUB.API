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
        public ProjectManagementType ProjectManagementType { get; set; }
        public string ProjectManagementTypeLabel => ProjectManagementType.ToString();
        public string ProjectType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Budget { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string PlantName { get; set; } = string.Empty;
        public string Sponsor { get; set; } = string.Empty;
        public decimal EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }
    }
}
