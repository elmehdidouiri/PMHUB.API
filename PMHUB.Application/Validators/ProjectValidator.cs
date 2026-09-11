using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;

namespace PMHUB.Application.Validators
{
    public class ProjectValidator
    {
        public static void ValidateForCreate(CreateFullProjectDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            AddDepartmentErrors(errors, dto.DepartmentId, dto.DepartmentIds);
            AddDateErrors(errors, dto.StartDate ?? DateTime.UtcNow.Date, dto.EndDate, dto.EstimatedDueDate);
            AddPhaseStatusErrors(errors, dto.Phase, dto.Status);
            AddEstimatedStartDateErrors(errors, dto.Status, dto.EstimatedStartDate);

            if (dto.ProjectType.HasValue)
            {
                AddParentProjectErrors(errors, dto.ProjectType.Value, dto.ParentProjectId);
            }

            ThrowIfAny(errors);
        }

        public static void ValidateForUpdate(Guid projectId, UpdateProjectDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            AddDepartmentErrors(errors, dto.DepartmentId, dto.DepartmentIds);

            if (dto.StartDate.HasValue)
            {
                AddDateErrors(errors, dto.StartDate.Value, dto.EndDate, dto.EstimatedDueDate);
            }

            if (dto.Phase.HasValue && dto.Status.HasValue)
            {
                AddPhaseStatusErrors(errors, dto.Phase.Value, dto.Status.Value);
            }

            if (dto.Status.HasValue)
            {
                AddEstimatedStartDateErrors(errors, dto.Status.Value, dto.EstimatedStartDate);
            }

            if (dto.ProjectType.HasValue)
            {
                AddParentProjectErrors(errors, dto.ProjectType.Value, dto.ParentProjectId);
            }

            if (dto.ParentProjectId.HasValue && dto.ParentProjectId.Value == projectId)
            {
                AddError(errors, "parentProjectId", "A project cannot reference itself as its parent project.");
            }

            ThrowIfAny(errors);
        }

        public static void ValidateDates(CreateFullProjectDto dto)
        {
            var startDate = dto.StartDate ?? DateTime.UtcNow.Date;

            if (dto.EndDate.HasValue && dto.EndDate <= startDate)
            {
                throw new BadRequestException(
                    "The project end date must be greater than the project start date.");
            }

            if (dto.EstimatedDueDate.HasValue && dto.EstimatedDueDate <= startDate)
            {
                throw new BadRequestException(
                    "The estimated due date must be greater than the project start date.");
            }
        }

        public static void ValidateDates(UpdateProjectDto dto)
        {
            if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate <= dto.StartDate)
            {
                throw new BadRequestException(
                    "The project end date must be greater than the project start date.");
            }

            if (dto.StartDate.HasValue && dto.EstimatedDueDate.HasValue && dto.EstimatedDueDate <= dto.StartDate)
            {
                throw new BadRequestException(
                    "The estimated due date must be greater than the project start date.");
            }
        }

        public static void ValidatePhaseStatus(ProjectPhase phase, ProjectStatus status)
        {
            var allowedStatuses = GetAllowedStatuses(phase);

            if (!allowedStatuses.Contains(status))
            {
                throw new BadRequestException(
                    $"Status '{status}' is not valid for phase '{phase}'.");
            }
        }

        public static void ValidateEstimatedStartDate(ProjectStatus status, DateTime? estimatedStartDate)
        {
            if (status == ProjectStatus.OnHold && !estimatedStartDate.HasValue)
            {
                throw new BadRequestException("An estimated start date is required when a project is on hold.");
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
            dto.DepartmentIds = parentProject.ProjectDepartments.Any()
                ? parentProject.ProjectDepartments.Select(pd => pd.DepartmentId).ToList()
                : new List<Guid> { parentProject.DepartmentId };
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

        private static void AddDepartmentErrors(
            IDictionary<string, string[]> errors,
            Guid? departmentId,
            IEnumerable<Guid>? departmentIds)
        {
            var hasDepartmentId = departmentId.HasValue && departmentId.Value != Guid.Empty;
            var hasDepartmentIds = (departmentIds ?? Enumerable.Empty<Guid>())
                .Any(id => id != Guid.Empty);

            if (!hasDepartmentId && !hasDepartmentIds)
            {
                AddError(errors, "departmentIds", "At least one department is required.");
            }
        }

        private static void AddDateErrors(
            IDictionary<string, string[]> errors,
            DateTime startDate,
            DateTime? endDate,
            DateTime? estimatedDueDate)
        {
            if (endDate.HasValue && endDate <= startDate)
            {
                AddError(errors, "endDate", "The project end date must be greater than the project start date.");
            }

            if (estimatedDueDate.HasValue && estimatedDueDate <= startDate)
            {
                AddError(errors, "estimatedDueDate", "The estimated due date must be greater than the project start date.");
            }
        }

        private static void AddPhaseStatusErrors(
            IDictionary<string, string[]> errors,
            ProjectPhase phase,
            ProjectStatus status)
        {
            var allowedStatuses = GetAllowedStatuses(phase);

            if (!allowedStatuses.Contains(status))
            {
                AddError(errors, "status", $"Status '{status}' is not valid for phase '{phase}'.");
            }
        }

        private static void AddEstimatedStartDateErrors(
            IDictionary<string, string[]> errors,
            ProjectStatus status,
            DateTime? estimatedStartDate)
        {
            if (status == ProjectStatus.OnHold && !estimatedStartDate.HasValue)
            {
                AddError(errors, "estimatedStartDate", "An estimated start date is required when a project is on hold.");
            }
        }

        private static void AddParentProjectErrors(
            IDictionary<string, string[]> errors,
            ProjectType projectType,
            Guid? parentProjectId)
        {
            var usesParentProject = projectType is ProjectType.NewPhase or ProjectType.Extension or ProjectType.Sustain;

            if (usesParentProject && (!parentProjectId.HasValue || parentProjectId.Value == Guid.Empty))
            {
                AddError(errors, "parentProjectId", "Parent project is required for this project type.");
            }

            if (!usesParentProject && parentProjectId.HasValue && parentProjectId.Value != Guid.Empty)
            {
                AddError(errors, "parentProjectId", "Parent project is only allowed for NewPhase, Extension, or Sustain projects.");
            }
        }

        private static ProjectStatus[] GetAllowedStatuses(ProjectPhase phase)
        {
            return phase switch
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
        }

        private static void AddError(IDictionary<string, string[]> errors, string field, string message)
        {
            if (errors.TryGetValue(field, out var existing))
            {
                errors[field] = existing.Append(message).ToArray();
                return;
            }

            errors[field] = new[] { message };
        }

        private static void ThrowIfAny(IDictionary<string, string[]> errors)
        {
            if (errors.Count > 0)
            {
                throw new ValidationException(errors);
            }
        }
    }
}
