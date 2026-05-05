using System;

namespace PMHUB.Application.DTOs
{
    public class DashboardQueryDto
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
        public Guid? RoleId { get; set; }
        public string? ProjectStatus { get; set; }
        public string? ProjectPhase { get; set; }
        public string? ProcessStatus { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? BusinessUnitId { get; set; }
        public Guid? PlantId { get; set; }
        public Guid? ProjectManagerId { get; set; }
        public string? ProjectType { get; set; }
        public string? ProjectManagementType { get; set; }
        public bool Ytd { get; set; }
        public int TopN { get; set; } = 5;
    }
}
