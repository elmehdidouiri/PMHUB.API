 using System;

namespace PMHUB.Domain.ValueObjects
{
    public class HourBreakdown : IEquatable<HourBreakdown>
    {
        public decimal ExecutionHours { get; }
        public decimal SupervisionHours { get; }
        public decimal ProcessHours { get; }
        public decimal ManagementHours { get; }
        public decimal RAndDHours { get; }
        public decimal WorkshopHours { get; }
        public decimal OtherHours { get; }
        public decimal InternManagementHours { get; }

        public decimal Total => ExecutionHours + SupervisionHours + ProcessHours +
                               ManagementHours + RAndDHours + WorkshopHours + OtherHours + InternManagementHours;

        private HourBreakdown(
            decimal executionHours,
            decimal supervisionHours,
            decimal processHours,
            decimal managementHours,
            decimal rAndDHours,
            decimal workshopHours,
            decimal otherHours,
            decimal internManagementHours)
        {
            ValidateHours(executionHours, nameof(executionHours));
            ValidateHours(supervisionHours, nameof(supervisionHours));
            ValidateHours(processHours, nameof(processHours));
            ValidateHours(managementHours, nameof(managementHours));
            ValidateHours(rAndDHours, nameof(rAndDHours));
            ValidateHours(workshopHours, nameof(workshopHours));
            ValidateHours(otherHours, nameof(otherHours));
            ValidateHours(internManagementHours, nameof(internManagementHours));

            ExecutionHours = executionHours;
            SupervisionHours = supervisionHours;
            ProcessHours = processHours;
            ManagementHours = managementHours;
            RAndDHours = rAndDHours;
            WorkshopHours = workshopHours;
            OtherHours = otherHours;
            InternManagementHours = internManagementHours;

            if (Total > 24)
                throw new ArgumentException(
                    $"Le total des heures ({Total}h) ne peut pas dépasser 24h par jour");
        }

        public static HourBreakdown Create(
            decimal executionHours = 0,
            decimal supervisionHours = 0,
            decimal processHours = 0,
            decimal managementHours = 0,
            decimal rAndDHours = 0,
            decimal workshopHours = 0,
            decimal otherHours = 0,
            decimal internManagementHours = 0)
        {
            return new HourBreakdown(
                executionHours,
                supervisionHours,
                processHours,
                managementHours,
                rAndDHours,
                workshopHours,
                otherHours,
                internManagementHours);
        }

        public static HourBreakdown Zero()
        {
            return new HourBreakdown(0, 0, 0, 0, 0, 0, 0, 0);
        }

        private static void ValidateHours(decimal hours, string paramName)
        {
            if (hours < 0)
                throw new ArgumentException(
                    $"Les heures ne peuvent pas être négatives", paramName);

            if (hours > 24)
                throw new ArgumentException(
                    $"Les heures ne peuvent pas dépasser 24h", paramName);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as HourBreakdown);
        }

        public bool Equals(HourBreakdown? other)
        {
            return other != null &&
                   ExecutionHours == other.ExecutionHours &&
                   SupervisionHours == other.SupervisionHours &&
                   ProcessHours == other.ProcessHours &&
                   ManagementHours == other.ManagementHours &&
                   RAndDHours == other.RAndDHours &&
                   WorkshopHours == other.WorkshopHours &&
                   OtherHours == other.OtherHours &&
                   InternManagementHours == other.InternManagementHours;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                ExecutionHours,
                SupervisionHours,
                ProcessHours,
                ManagementHours,
                RAndDHours,
                WorkshopHours,
                OtherHours,
                InternManagementHours);
        }

        public override string ToString()
        {
            return $"Total: {Total}h (Exec: {ExecutionHours}h, Sup: {SupervisionHours}h, " +
                   $"Proc: {ProcessHours}h, Mgmt: {ManagementHours}h, " +
                   $"R&D: {RAndDHours}h, Workshop: {WorkshopHours}h, Other: {OtherHours}h, Intern: {InternManagementHours}h)";
        }
    }
}