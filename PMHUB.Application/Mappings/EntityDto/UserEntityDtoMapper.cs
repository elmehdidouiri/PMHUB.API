using PMHUB.Application.DTOs;
using PMHUB.Domain.Entities;

namespace PMHUB.Application.Mappings.EntityDto;

public static class UserEntityDtoMapper
{
    public static UserDto ToDto(User user)
    {
        var normalUser = user as NormalUser;

        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            RoleId = normalUser?.RoleId ?? Guid.Empty,
            RoleName = normalUser?.Role?.Name,
            IsApproved = normalUser?.IsApproved ?? false,
            IsActive = normalUser?.IsActive ?? false,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}