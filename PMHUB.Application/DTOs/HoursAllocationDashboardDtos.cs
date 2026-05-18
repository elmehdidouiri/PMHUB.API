namespace PMHUB.Application.DTOs
{
    public class HoursAllocationDashboardQueryDto
    {
        public Guid? UserId { get; set; }
        public Guid? MemberId { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? RoleId { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? QuickSelect { get; set; }
        public string? Analysis { get; set; }
    }

    public class HoursAllocationReminderRequestDto : HoursAllocationDashboardQueryDto
    {
    }

    public class HoursAllocationReminderResultDto
    {
        public int Sent { get; set; }
        public List<string> Recipients { get; set; } = new();
    }

    public class HoursAllocationFiltersDto
    {
        public List<HoursAllocationOptionDto<Guid>> Users { get; set; } = new();
        public List<HoursAllocationOptionDto<Guid>> Projects { get; set; } = new();
        public List<HoursAllocationOptionDto<Guid>> Roles { get; set; } = new();
        public List<HoursAllocationOptionDto<Guid>> Members { get; set; } = new();
        public List<int> FiscalYears { get; set; } = new();
        public List<HoursAllocationMonthOptionDto> Months { get; set; } = new();
    }

    public class HoursAllocationOptionDto<T>
    {
        public T Id { get; set; } = default!;
        public string Label { get; set; } = string.Empty;
    }

    public class HoursAllocationMonthOptionDto
    {
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class HoursAllocationDashboardDto
    {
        public HoursAllocationSummaryDto Summary { get; set; } = new();
        public List<HoursAllocationDetailDto> Details { get; set; } = new();
        public List<HoursAllocationMonthlyBreakdownDto> MonthlyBreakdown { get; set; } = new();
        public List<HoursAllocationByUserDto> HoursByUser { get; set; } = new();
        public List<HoursAllocationByProjectDto> HoursByProject { get; set; } = new();
        public List<HoursAllocationByRoleDto> HoursByRole { get; set; } = new();
        public List<HoursAllocationByTeamDto> HoursByTeam { get; set; } = new();
    }

    public class HoursAllocationSummaryDto
    {
        public decimal TotalHours { get; set; }
        public int ActiveUsers { get; set; }
        public int Projects { get; set; }
        public int Allocations { get; set; }
        public decimal AverageUtilization { get; set; }
        public decimal YearToDateHours { get; set; }
        public decimal AverageMonthlyHours { get; set; }
        public int WorkedDays { get; set; }
    }

    public class HoursAllocationDetailDto
    {
        public DateTime Date { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid? ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal ExecutionHours { get; set; }
        public decimal TechLeadHours { get; set; }
        public decimal ProcessHours { get; set; }
        public decimal ProjectManagementHours { get; set; }
        public decimal ResearchAndDevHours { get; set; }
        public decimal WorkshopHours { get; set; }
        public decimal OtherHours { get; set; }
        public decimal TotalHours { get; set; }
        public bool IsProjectManager { get; set; }
    }

    public class HoursAllocationMonthlyBreakdownDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal UtilizationPercentage { get; set; }
    }

    public class HoursAllocationByUserDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal AllocatedHours { get; set; }
        public string DurationLabel { get; set; } = string.Empty;
        public decimal RemainingHours { get; set; }
        public decimal AvailableHours { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public decimal ExecutionHours { get; set; }
        public decimal TechLeadHours { get; set; }
        public decimal ProcessHours { get; set; }
        public decimal ProjectManagementHours { get; set; }
        public decimal ResearchAndDevHours { get; set; }
        public decimal WorkshopHours { get; set; }
        public int ProjectCount { get; set; }
        public int AllocationCount { get; set; }
    }

    public class HoursAllocationByProjectDto
    {
        public Guid? ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal ProjectManagerHours { get; set; }
        public decimal TeamHours { get; set; }
        public int TeamMembers { get; set; }
        public int Allocations { get; set; }
    }

    public class HoursAllocationByRoleDto
    {
        public Guid RoleId { get; set; }
        public string Role { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public int TeamMembers { get; set; }
        public decimal Percentage { get; set; }
    }

    public class HoursAllocationByTeamDto
    {
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public int WorkedDays { get; set; }
        public int AllocationCount { get; set; }
    }

    public class HoursAllocationReminderRecipientDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
    }
}
