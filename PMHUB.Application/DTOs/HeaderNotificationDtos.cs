namespace PMHUB.Application.DTOs
{
    public class HeaderNotificationSummaryDto
    {
        public int TotalCount { get; set; }
        public int CriticalCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ICollection<HeaderNotificationGroupDto> Groups { get; set; } = new List<HeaderNotificationGroupDto>();
        public ICollection<HeaderNotificationDto> Items { get; set; } = new List<HeaderNotificationDto>();
    }

    public class HeaderNotificationGroupDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public int CriticalCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
    }

    public class HeaderNotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = "info";
        public string GroupKey { get; set; } = string.Empty;
        public string GroupLabel { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? OccurredAt { get; set; }
        public string TargetType { get; set; } = string.Empty;
        public Guid TargetId { get; set; }
        public Guid? ProjectId { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public IDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();
    }

    public class HeaderNotificationStateChangeDto
    {
        public int AffectedCount { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
