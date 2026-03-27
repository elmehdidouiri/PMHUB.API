// Fichier : PMHUB.Domain/Entities/HourEntry.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using PMHUB.Domain.Enums;
using PMHUB.Domain.ValueObjects;

namespace PMHUB.Domain.Entities
{
    public class HourEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public NormalUser User { get; set; } = null!;

        [Required]
        public Guid ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project Project { get; set; } = null!;

        [Required]
        public AllocationType AllocationType { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public Guid? WeekBatchId { get; set; }

        [Required]
        public ProjectType ProjectType { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal ExecutionHours { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal SupervisionHours { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal ProcessHours { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal ManagementHours { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal RAndDHours { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal WorkshopHours { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal OtherHours { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalHours { get; private set; }

        public bool IsPremium { get; private set; }

        public PremiumReason? PremiumReason { get; private set; }

        public ApprovalStatus? PremiumApprovalStatus { get; private set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal HourlyRateAmount { get; private set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "EUR";

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalCost { get; private set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public HourBreakdown GetHourBreakdown()
        {
            return HourBreakdown.Create(
                ExecutionHours,
                SupervisionHours,
                ProcessHours,
                ManagementHours,
                RAndDHours,
                WorkshopHours,
                OtherHours);
        }

        public void Calculate(UserHourlyRate userRate, IEnumerable<Holiday> holidays)
        {
            if (userRate == null)
                throw new ArgumentNullException(nameof(userRate));

            var breakdown = GetHourBreakdown();
            TotalHours = breakdown.Total;

            DeterminePremiumStatus(holidays);

            var rate = (IsPremium && PremiumApprovalStatus == ApprovalStatus.Approved)
                ? userRate.GetPremiumRate()
                : userRate.GetNormalRate();

            HourlyRateAmount = rate.Amount;
            Currency = rate.Currency;

            var cost = rate.Multiply(TotalHours);
            TotalCost = cost.Amount;

            UpdatedAt = DateTime.UtcNow;
        }

        private void DeterminePremiumStatus(IEnumerable<Holiday> holidays)
        {
            var dayOfWeek = Date.DayOfWeek;
            var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
            var isHoliday = holidays.Any(h => h.Date.Date == Date.Date && h.IsActive);

            if (isWeekend)
            {
                IsPremium = true;
                PremiumReason = Enums.PremiumReason.Weekend;
                PremiumApprovalStatus = ApprovalStatus.Pending;
            }
            else if (isHoliday)
            {
                IsPremium = true;
                PremiumReason = Enums.PremiumReason.Holiday;
                PremiumApprovalStatus = ApprovalStatus.Pending;
            }
            else if (TotalHours > 9m)
            {
                IsPremium = true;
                PremiumReason = Enums.PremiumReason.Overtime;
                PremiumApprovalStatus = ApprovalStatus.Pending;
            }
            else
            {
                IsPremium = false;
                PremiumReason = null;
                PremiumApprovalStatus = null;
            }
        }

        public void ApplyPremiumDecision(bool approved, UserHourlyRate userRate)
        {
            if (!IsPremium)
                throw new InvalidOperationException("Cette entrée n'est pas premium.");

            if (PremiumApprovalStatus != ApprovalStatus.Pending)
                throw new InvalidOperationException("Cette entrée a déjà été traitée.");

            PremiumApprovalStatus = approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;

            var rate = approved ? userRate.GetPremiumRate() : userRate.GetNormalRate();
            HourlyRateAmount = rate.Amount;
            Currency = rate.Currency;

            var cost = rate.Multiply(TotalHours);
            TotalCost = cost.Amount;

            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsPendingPremiumApproval()
        {
            return IsPremium && PremiumApprovalStatus == ApprovalStatus.Pending;
        }
    }
}