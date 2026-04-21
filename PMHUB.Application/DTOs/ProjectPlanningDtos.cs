using Microsoft.AspNetCore.Http;
using PMHUB.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class DeliverableBreakdownDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DeliverablePriority Priority { get; set; }
        public string PriorityLabel => Priority.ToString();
        public decimal EstimatedHours { get; set; }
        public decimal DevelopmentSubtotal { get; set; }
        public decimal InfrastructureAndSetupSubtotal { get; set; }
        public decimal DesignAndUxSubtotal { get; set; }
        public decimal TestingAndQualityAssuranceSubtotal { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<DeliverableTaskDto> Tasks { get; set; } = new List<DeliverableTaskDto>();
    }

    public class DeliverableTaskDto
    {
        public Guid Id { get; set; }
        public Guid DeliverableId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DeliverableTaskCategory Category { get; set; }
        public string CategoryLabel => Category.ToString();
        public int? StoryPoints { get; set; }
        public decimal? DevHours { get; set; }
        public decimal? UxHours { get; set; }
        public decimal? TestingHours { get; set; }
        public decimal? Hours { get; set; }
        public decimal EstimatedHours { get; set; }
        public TaskStatuss Status { get; set; }
        public string StatusLabel => Status.ToString();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
        public Guid? ProjectMemberId { get; set; }
        public string? AssignedProjectMemberName { get; set; }
        public Guid? InternAllocationId { get; set; }
        public string? AssignedInternName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateDeliverableBreakdownDto
    {
        [Required(ErrorMessage = "Deliverable name is required.")]
        [MaxLength(200, ErrorMessage = "Deliverable name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Priority is required.")]
        public DeliverablePriority Priority { get; set; }
    }

    public class UpdateDeliverableBreakdownDto
    {
        [Required(ErrorMessage = "Deliverable name is required.")]
        [MaxLength(200, ErrorMessage = "Deliverable name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Priority is required.")]
        public DeliverablePriority Priority { get; set; }
    }

    public class CreateDeliverableTaskDto
    {
        [Required(ErrorMessage = "Task name is required.")]
        [MaxLength(200, ErrorMessage = "Task name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public DeliverableTaskCategory Category { get; set; }

        [Range(0, int.MaxValue)]
        public int? StoryPoints { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DevHours { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? UxHours { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? TestingHours { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Hours { get; set; }

        public TaskStatuss Status { get; set; } = TaskStatuss.NotStarted;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public Guid? ProjectMemberId { get; set; }
        public Guid? InternAllocationId { get; set; }
    }

    public class UpdateDeliverableTaskDto
    {
        [Required(ErrorMessage = "Task name is required.")]
        [MaxLength(200, ErrorMessage = "Task name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public DeliverableTaskCategory Category { get; set; }

        [Range(0, int.MaxValue)]
        public int? StoryPoints { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DevHours { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? UxHours { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? TestingHours { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Hours { get; set; }

        public TaskStatuss Status { get; set; } = TaskStatuss.NotStarted;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public Guid? ProjectMemberId { get; set; }
        public Guid? InternAllocationId { get; set; }
    }

    public class ProjectTimelineEntryDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public DateTime Date { get; set; }
        public ProjectPhase Phase { get; set; }
        public string PhaseEnumLabel => Phase.ToString();
        public int PhaseOrder { get; set; }
        public string PhaseLabel { get; set; } = string.Empty;
        public Guid SponsorUserId { get; set; }
        public string SponsorUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateProjectTimelineEntryDto
    {
        [Required(ErrorMessage = "Date is required.")]
        public DateTime? Date { get; set; }

        [Required(ErrorMessage = "Phase is required.")]
        public ProjectPhase? Phase { get; set; }

        [Required(ErrorMessage = "Phase order is required.")]
        public int? PhaseOrder { get; set; }

        [Required(ErrorMessage = "Phase label is required.")]
        [MaxLength(200, ErrorMessage = "Phase label cannot exceed 200 characters.")]
        public string PhaseLabel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sponsor user ID is required.")]
        public Guid? SponsorUserId { get; set; }
    }

    public class UpdateProjectTimelineEntryDto
    {
        [Required(ErrorMessage = "Date is required.")]
        public DateTime? Date { get; set; }

        [Required(ErrorMessage = "Phase is required.")]
        public ProjectPhase? Phase { get; set; }

        [Required(ErrorMessage = "Phase order is required.")]
        public int? PhaseOrder { get; set; }

        [Required(ErrorMessage = "Phase label is required.")]
        [MaxLength(200, ErrorMessage = "Phase label cannot exceed 200 characters.")]
        public string PhaseLabel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sponsor user ID is required.")]
        public Guid SponsorUserId { get; set; }
    }

    public class ProjectRoadblockDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public RoadblockStatus Status { get; set; }
        public string StatusLabel => Status.ToString();
        public DateTime EnteredAt { get; set; }
        public DateTime DueAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public bool IsDelayed => Status != RoadblockStatus.Resolved && DueAt < DateTime.UtcNow;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateProjectRoadblockDto
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public RoadblockStatus Status { get; set; } = RoadblockStatus.Open;

        public DateTime? EnteredAt { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        public DateTime? DueAt { get; set; }

        public DateTime? ResolvedAt { get; set; }
    }

    public class UpdateProjectRoadblockDto
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public RoadblockStatus Status { get; set; } = RoadblockStatus.Open;

        public DateTime? EnteredAt { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        public DateTime? DueAt { get; set; }

        public DateTime? ResolvedAt { get; set; }
    }

    public class ProjectFileVersionDto
    {
        public Guid Id { get; set; }
        public Guid ProjectFileId { get; set; }
        public int VersionNumber { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UploadProjectFileVersionDto
    {
        [Required(ErrorMessage = "File is required.")]
        public IFormFile File { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
