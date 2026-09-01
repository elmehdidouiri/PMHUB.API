using PMHUB.Application.DTOs;
using PMHUB.Domain.Entities;

namespace PMHUB.Application.Mappings
{
    public static class ProjectInternMapper
    {
        public static ProjectInternAllocationDto ToDto(InternAllocation allocation) => new()
        {
            InternAllocationId = allocation.Id,
            InternId = allocation.InternId,
            InternName = allocation.Intern?.Name ?? string.Empty,
            RoleId = allocation.Intern?.RoleId ?? Guid.Empty,
            RoleName = allocation.Intern?.Role?.Name ?? string.Empty,
            SupervisorId = allocation.Intern?.SupervisorId ?? Guid.Empty,
            SupervisorName = allocation.Intern?.Supervisor is null
                ? string.Empty
                : $"{allocation.Intern.Supervisor.FirstName} {allocation.Intern.Supervisor.LastName}".Trim(),
            SupervisorEmail = allocation.Intern?.Supervisor?.Email ?? string.Empty,
            AllocatedHours = allocation.AllocatedHours,
            HoursWorked = allocation.InternHourEntries.Sum(e => e.Hours),
            RemainingHours = Math.Max(0m, allocation.AllocatedHours - allocation.InternHourEntries.Sum(e => e.Hours)),
            AllocationDate = allocation.AllocationDate,
            Notes = allocation.Notes,
            CreatedAt = allocation.CreatedAt,
            UpdatedAt = allocation.UpdatedAt,
            HourEntries = allocation.InternHourEntries
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.CreatedAt)
                .Select(ToDto)
                .ToList()
        };

        public static InternHourEntryDto ToDto(InternHourEntry entry) => new()
        {
            Id = entry.Id,
            InternAllocationId = entry.InternAllocationId,
            BookedByUserId = entry.BookedByUserId,
            BookedByUserName = entry.BookedByUser is null
                ? string.Empty
                : $"{entry.BookedByUser.FirstName} {entry.BookedByUser.LastName}".Trim(),
            Date = entry.Date,
            Hours = entry.Hours,
            Notes = entry.Notes,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }
}
