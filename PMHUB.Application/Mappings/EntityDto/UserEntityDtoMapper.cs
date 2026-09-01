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
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Email = user.Email,
            RoleId = normalUser?.RoleId,
            RoleName = normalUser?.Role?.Name ?? string.Empty,
            IsAdmin = user is Admin,
            IsApproved = normalUser?.IsApproved ?? false,
            IsActive = normalUser?.IsActive ?? false,
            MemberType = normalUser?.MemberType,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
