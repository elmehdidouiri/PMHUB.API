using PMHUB.Domain.Enums;

namespace PMHUB.Application.DTOs
{
    public class BookingActivityNotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public Guid HourEntryId { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public Guid? ProjectId { get; set; }
        public string ActivityTarget { get; set; } = string.Empty;
        public CategoryWork Category { get; set; }
        public AllocationType AllocationType { get; set; }
        public string BookingType { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public decimal TotalHours { get; set; }
        public decimal ExecutionHours { get; set; }
        public decimal SupervisionHours { get; set; }
        public decimal ProcessHours { get; set; }
        public decimal ManagementHours { get; set; }
        public decimal RAndDHours { get; set; }
        public decimal WorkshopHours { get; set; }
        public decimal OtherHours { get; set; }
        public decimal InternManagementHours { get; set; }
        public string? Notes { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ActionUrl { get; set; } = string.Empty;
    }
}
