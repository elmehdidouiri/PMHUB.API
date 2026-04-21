using PMHUB.Application.DTOs;
using PMHUB.Domain.Entities;

namespace PMHUB.Application.Mappings.EntityDto;

public static class InternEntityDtoMapper
{
    public static InternDto ToDto(Intern intern, NormalUser? supervisor, Role? role) => new()
    {
        Id = intern.Id,
        Name = intern.Name,
        RoleId = intern.RoleId,
        RoleName = role?.Name ?? string.Empty,
        SupervisorId = intern.SupervisorId,
        SupervisorName = supervisor is null ? string.Empty : $"{supervisor.FirstName} {supervisor.LastName}".Trim(),
        SupervisorEmail = supervisor?.Email ?? string.Empty,
        CreatedAt = intern.CreatedAt,
        UpdatedAt = intern.UpdatedAt
    };
}