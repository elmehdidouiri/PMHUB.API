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
                throw new BadRequestException(
                    "La date de fin doit être supérieure à la date de début.");
            if (dto.EstimatedDueDate.HasValue && dto.EstimatedDueDate <= dto.StartDate)
                throw new BadRequestException(
                    "La date estimée doit être supérieure à la date de début.");
        }

        public static void ValidateDates(UpdateProjectDto dto)
        {
            if (dto.EndDate.HasValue && dto.EndDate <= dto.StartDate)
                throw new BadRequestException(
                    "La date de fin doit être supérieure à la date de début.");
            if (dto.EstimatedDueDate.HasValue && dto.EstimatedDueDate <= dto.StartDate)
                throw new BadRequestException(
                    "La date estimée doit être supérieure à la date de début.");
        }

         public static void ValidateManagementType(
            CreateFullProjectDto dto, Project? parentProject)
        {
            switch (dto.ProjectManagementType)
            {
                case ProjectManagementType.NewProject:
                case ProjectManagementType.Sustain:
                    if (dto.ParentProjectId.HasValue)
                        throw new BadRequestException(
                            $"Un projet '{dto.ProjectManagementType}' ne peut pas avoir un parent.");
                    break;

                case ProjectManagementType.NewPhase:
                case ProjectManagementType.Extension:
                    if (!dto.ParentProjectId.HasValue)
                        throw new BadRequestException(
                            $"Un projet '{dto.ProjectManagementType}' doit avoir un projet parent.");
                    if (parentProject == null)
                        throw new NotFoundException("ParentProject", dto.ParentProjectId!.Value);
                    break;

                case ProjectManagementType.NewProcessProject:
                    if (dto.ProcessStatus == ProcessStatus.NotStarted)
                        throw new BadRequestException(
                            "Un projet 'Nouveau Processus' doit avoir un statut défini.");
                    break;
            }
        }

         public static void InheritFromParent(CreateFullProjectDto dto, Project parentProject)
        {
            dto.DepartmentId = parentProject.DepartmentId;
            dto.BusinessUnitIds = parentProject.ProjectBusinessUnits
                .Select(pbu => pbu.BusinessUnitId).ToList();

             dto.Members = parentProject.ProjectMembers
                .Select(pm => new CreateProjectMemberDto
                {
                    UserId = pm.UserId,
                    RoleId = pm.RoleId
                }).ToList();
        }
    }
}