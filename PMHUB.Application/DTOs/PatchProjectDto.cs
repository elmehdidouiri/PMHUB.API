using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace PMHUB.Application.DTOs
{
    public class PatchProjectDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Guid? DepartmentId { get; set; }
        public ICollection<Guid>? DepartmentIds { get; set; }
        public ProjectStatus? Status { get; set; }
        public ProjectPhase? Phase { get; set; }
        public ProcessStatus? ProcessStatus { get; set; }
        public Category? ProjectManagementType { get; set; }
        public ProjectType? ProjectType { get; set; }
        public Guid? ParentProjectId { get; set; }
        public int? ProgressPercentage { get; set; }
        public string? CurrentState { get; set; }
        public string? NextSteps { get; set; }
        public string? Enhancements { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? EstimatedDueDate { get; set; }
        public decimal? Budget { get; set; }
        public decimal? CostSaving { get; set; }
        public decimal? EstimatedHours { get; set; }
        public decimal? ActualHours { get; set; }
        public string? Sponsor { get; set; }
        public Guid? ProjectManagerId { get; set; }
        public string? CodeSourceLink { get; set; }
        public string? SolutionLink { get; set; }
        public string? ServerHostName { get; set; }
        public string? CostCenter { get; set; }
        public ICollection<Guid>? BusinessUnitIds { get; set; }
        public ICollection<Guid>? TechnologyIds { get; set; }
        public ICollection<Guid>? SolutionDomainIds { get; set; }
        public ICollection<CreateStrategicCriterionDto>? StrategicCriteria { get; set; }
        public ICollection<CreateProjectResourceDto>? ProjectResources { get; set; }
        public ICollection<CreateKpiDto>? KPIs { get; set; }

        [JsonPropertyName("budgetItems")]
        public ICollection<CreateProjectResourceDto>? BudgetItems { get; set; }
        public ICollection<CreateProjectMemberDto>? Members { get; set; }

        [JsonPropertyName("teamMembers")]
        public ICollection<CreateProjectMemberDto>? TeamMembers { get; set; }
    }
}
