using PMHUB.Application.DTOs;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;

namespace PMHUB.Application.Mappings
{
    public static class ProjectPlanningMapper
    {
        public static DeliverableBreakdownDto ToDto(DeliverableBreakdown deliverable) => new()
        {
            Id = deliverable.Id,
            ProjectId = deliverable.ProjectId,
            Name = deliverable.Name,
            Description = deliverable.Description,
            Priority = deliverable.Priority,
            EstimatedHours = deliverable.Tasks.Sum(t => t.EstimatedHours),
            DevelopmentSubtotal = deliverable.Tasks
                .Where(t => t.Category == DeliverableTaskCategory.Development)
                .Sum(t => t.EstimatedHours),
            InfrastructureAndSetupSubtotal = deliverable.Tasks
                .Where(t => t.Category == DeliverableTaskCategory.InfrastructureAndSetup)
                .Sum(t => t.EstimatedHours),
            DesignAndUxSubtotal = deliverable.Tasks
                .Where(t => t.Category == DeliverableTaskCategory.DesignAndUx)
                .Sum(t => t.EstimatedHours),
            TestingAndQualityAssuranceSubtotal = deliverable.Tasks
                .Where(t => t.Category == DeliverableTaskCategory.TestingAndQualityAssurance)
                .Sum(t => t.EstimatedHours),
            CreatedAt = deliverable.CreatedAt,
            UpdatedAt = deliverable.UpdatedAt,
            Tasks = deliverable.Tasks
                .OrderBy(t => t.CreatedAt)
                .Select(ToDto)
                .ToList()
        };

        public static DeliverableTaskDto ToDto(DeliverableTask task) => new()
        {
            Id = task.Id,
            DeliverableId = task.DeliverableId,
            Name = task.Name,
            Description = task.Description,
            Category = task.Category,
            StoryPoints = task.StoryPoints,
            DevHours = task.DevHours,
            UxHours = task.UxHours,
            TestingHours = task.TestingHours,
            Hours = task.Hours,
            EstimatedHours = task.EstimatedHours,
            Status = task.Status,
            StartDate = task.StartDate,
            EndDate = task.EndDate,
            Notes = task.Notes,
            ProjectMemberId = task.ProjectMemberId,
            AssignedProjectMemberName = task.ProjectMember is null
                ? null
                : $"{task.ProjectMember.User?.FirstName} {task.ProjectMember.User?.LastName}".Trim(),
            InternAllocationId = task.InternAllocationId,
            AssignedInternName = task.InternAllocation?.Intern is null
                ? null
                : task.InternAllocation.Intern.Name,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };

        public static ProjectTimelineEntryDto ToDto(ProjectTimelineEntry entry) => new()
        {
            Id = entry.Id,
            ProjectId = entry.ProjectId,
            Date = entry.Date,
            Phase = entry.Phase,
            PhaseOrder = entry.PhaseOrder,
            PhaseLabel = entry.PhaseLabel,
            SponsorUserId = entry.SponsorUserId,
            SponsorUserName = $"{entry.SponsorUser.FirstName} {entry.SponsorUser.LastName}".Trim(),
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };

        public static ProjectRoadblockDto ToDto(ProjectRoadblock roadblock) => new()
        {
            Id = roadblock.Id,
            ProjectId = roadblock.ProjectId,
            Title = roadblock.Title,
            Description = roadblock.Description,
            Status = roadblock.Status,
            EnteredAt = roadblock.EnteredAt == default ? roadblock.CreatedAt : roadblock.EnteredAt,
            DueAt = roadblock.DueAt,
            ResolvedAt = roadblock.ResolvedAt,
            CreatedAt = roadblock.CreatedAt,
            UpdatedAt = roadblock.UpdatedAt
        };

        public static ProjectFileVersionDto ToDto(ProjectFileVersion version, IFileStorageService storageService) => new()
        {
            Id = version.Id,
            ProjectFileId = version.ProjectFileId,
            VersionNumber = version.VersionNumber,
            OriginalFileName = version.OriginalFileName,
            FileUrl = storageService.GetFileUrl(version.FilePath),
            ContentType = version.ContentType,
            FileSize = version.FileSize,
            Description = version.Description,
            CreatedAt = version.CreatedAt
        };
    }
}
