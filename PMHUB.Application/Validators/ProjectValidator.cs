using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;

namespace PMHUB.Application.Validators
{
    public class ProjectValidator
    {
        public static void ValidateDates(CreateFullProjectDto dto)
        {
            if (dto.EndDate.HasValue && dto.EndDate <= dto.StartDate)
            {
                throw new BadRequestException(
                    "The project end date must be greater than the project start date.");
            }

            if (dto.EstimatedDueDate.HasValue && dto.EstimatedDueDate <= dto.StartDate)
            {
                throw new BadRequestException(
                    "The estimated due date must be greater than the project start date.");
            }
        }

        public static void ValidateDates(UpdateProjectDto dto)
        {
            if (dto.EndDate.HasValue && dto.EndDate <= dto.StartDate)
            {
                throw new BadRequestException(
                    "The project end date must be greater than the project start date.");
            }

            if (dto.EstimatedDueDate.HasValue && dto.EstimatedDueDate <= dto.StartDate)
            {
                throw new BadRequestException(
                    "The estimated due date must be greater than the project start date.");
            }
        }

        public static void ValidatePhaseStatus(ProjectPhase phase, ProjectStatus status)
        {
            var allowedStatuses = phase switch
            {
                ProjectPhase.Pipeline => new[] { ProjectStatus.Planned, ProjectStatus.OnHold },
                ProjectPhase.PreProcess => new[] { ProjectStatus.Planned, ProjectStatus.Ongoing, ProjectStatus.OnHold },
                ProjectPhase.Initiation => new[] { ProjectStatus.Planned, ProjectStatus.Ongoing, ProjectStatus.OnHold },
                ProjectPhase.Planification => new[] { ProjectStatus.Planned, ProjectStatus.Ongoing, ProjectStatus.OnHold },
                ProjectPhase.Execution => new[] { ProjectStatus.Ongoing, ProjectStatus.OnHold },
                ProjectPhase.Monitoring => new[] { ProjectStatus.Ongoing, ProjectStatus.OnHold },
                ProjectPhase.Closing => new[] { ProjectStatus.Ongoing, ProjectStatus.Done },
                _ => Array.Empty<ProjectStatus>()
            };

            if (!allowedStatuses.Contains(status))
            {
                throw new BadRequestException(
                    $"Status '{status}' is not valid for phase '{phase}'.");
            }
        }

        public static void ValidateManagementType(CreateFullProjectDto dto, Project? parentProject)
        {
            if (dto.ParentProjectId.HasValue && parentProject == null)
            {
                throw new NotFoundException("ParentProject", dto.ParentProjectId.Value);
            }
        }

        public static void InheritFromParent(CreateFullProjectDto dto, Project parentProject)
        {
            dto.DepartmentId = parentProject.DepartmentId;
            dto.BusinessUnitIds = parentProject.ProjectBusinessUnits
                .Select(pbu => pbu.BusinessUnitId)
                .ToList();

            dto.Members = parentProject.ProjectMembers
                .Select(pm => new CreateProjectMemberDto
                {
                    UserId = pm.UserId,
                    RoleId = pm.RoleId
                })
                .ToList();
        }
    }
}
