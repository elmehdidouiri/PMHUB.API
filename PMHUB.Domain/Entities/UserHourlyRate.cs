 using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PMHUB.Domain.ValueObjects;

namespace PMHUB.Domain.Entities
{
    public class UserHourlyRate
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public NormalUser User { get; set; } = null!;

        [Column(TypeName = "decimal(10,2)")]
        public decimal NormalRateAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PremiumRateAmount { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "EUR";

        [Required]
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Money GetNormalRate() => Money.Create(NormalRateAmount, Currency);
        public Money GetPremiumRate() => Money.Create(PremiumRateAmount, Currency);

        public bool IsEffectiveOn(DateTime date)
        {
            return date >= EffectiveFrom &&
                   (!EffectiveTo.HasValue || date <= EffectiveTo.Value);
        }

        public void Deactivate()
        {
            IsActive = false;
            EffectiveTo = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}