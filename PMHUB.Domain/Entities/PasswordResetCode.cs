using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMHUB.Domain.Entities
{
    public class PasswordResetCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required, MaxLength(128)]
        public string CodeHash { get; set; } = string.Empty;

        [MaxLength(128)]
        public string? ResetTokenHash { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? ResetTokenExpiresAtUtc { get; set; }

        public int Attempts { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? VerifiedAtUtc { get; set; }

        public DateTime? ConsumedAtUtc { get; set; }
    }
}
