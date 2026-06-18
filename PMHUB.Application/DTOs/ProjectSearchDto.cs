using PMHUB.Domain.Enums;
using System;

namespace PMHUB.Application.DTOs
{
    public class ProjectSearchDto : PaginationQueryDto
    {
        public ProjectStatus? Status { get; set; }
        public ProjectPhase? Phase { get; set; }
        public ProjectType? ProjectType { get; set; }
        public string? ProjectStatus { get; set; }
        public string? ProjectPhase { get; set; }
        public string? ProcessStatus { get; set; }
        public string? ProjectManagementType { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? BusinessUnitId { get; set; }
        public Guid? PlantId { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? RoleId { get; set; }
        public Guid? ProjectManagerId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? InternId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? FiscalYear { get; set; }
        public string? ExportType { get; set; }
        public bool Ytd { get; set; } = false;
        public bool All { get; set; } = false;
        public bool DelayedOnly { get; set; } = false;
        public bool IncompleteOnly { get; set; } = false;
    }
}
