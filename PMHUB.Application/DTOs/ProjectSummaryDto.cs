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
        public ProjectStatus Status { get; set; }
        public string StatusLabel => Status.ToString();
        public ProjectPhase Phase { get; set; }
        public string PhaseLabel => Phase.ToString();
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Budget { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string PlantName { get; set; } = string.Empty;
    }
}
