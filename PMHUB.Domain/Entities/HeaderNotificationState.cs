using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMHUB.Domain.Entities
{
    public class HeaderNotificationState
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required, MaxLength(200)]
        public string NotificationId { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public bool IsDismissed { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAtUtc { get; set; }

        public DateTime? DismissedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
