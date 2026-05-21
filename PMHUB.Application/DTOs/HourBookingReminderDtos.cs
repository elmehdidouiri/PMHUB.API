namespace PMHUB.Application.DTOs
{
    public class HourBookingReminderSettings
    {
        public bool IsEnabled { get; set; } = true;
        public string WeeklyReminderDay { get; set; } = "Monday";
        public TimeSpan WeeklyReminderTime { get; set; } = new(9, 0, 0);
        public int NoBookingThresholdDays { get; set; } = 30;
    }

    public class HourBookingReminderResultDto
    {
        public int TotalUsers { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
    }

    public class AdminHourBookingNotificationDto
    {
        public Guid UserId { get; set; }
        public string TargetType { get; set; } = "User";
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid? SupervisorId { get; set; }
        public string? SupervisorName { get; set; }
        public string? SupervisorEmail { get; set; }
        public DateTime? LastBookingDate { get; set; }
        public int DaysWithoutBooking { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AdminMonthlyTargetNotificationDto
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal BookedHours { get; set; }
        public decimal TargetHours { get; set; }
        public decimal MissingHours { get; set; }
        public decimal CompletionRate { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
