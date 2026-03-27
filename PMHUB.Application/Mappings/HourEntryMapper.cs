 using PMHUB.Application.DTOs;
using PMHUB.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMHUB.Application.Mappings
{
    public static class HourEntryMapper
    {
 
        public static HourEntryDto ToDto(
            this HourEntry entry,
            string projectName,
            string userFullName)
        {
            ArgumentNullException.ThrowIfNull(entry);

            return new HourEntryDto
            {
                Id = entry.Id,
                UserId = entry.UserId,
                UserFullName = userFullName ?? "N/A",
                ProjectId = entry.ProjectId,
                ProjectName = projectName ?? "N/A",
                AllocationType = entry.AllocationType,
                ProjectType = entry.ProjectType,
                Date = entry.Date,

                ExecutionHours = entry.ExecutionHours,
                SupervisionHours = entry.SupervisionHours,
                ProcessHours = entry.ProcessHours,
                ManagementHours = entry.ManagementHours,
                RAndDHours = entry.RAndDHours,
                WorkshopHours = entry.WorkshopHours,
                OtherHours = entry.OtherHours,

                TotalHours = entry.TotalHours,

                 HourlyRate = entry.HourlyRateAmount,
                TotalCost = entry.TotalCost,
                Currency = entry.Currency,

                IsPremium = entry.IsPremium,
                PremiumReason = entry.PremiumReason?.ToString(),
                PremiumApprovalStatus = entry.PremiumApprovalStatus?.ToString(),

                Notes = entry.Notes,
                CreatedAt = entry.CreatedAt,
                UpdatedAt = entry.UpdatedAt
            };
        }
 
        public static HourEntryDto ToDto(this HourEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var projectName = entry.Project?.Name ?? "N/A";
            var userFullName = entry.User != null
                ? $"{entry.User.FirstName} {entry.User.LastName}".Trim()
                : "N/A";

            return entry.ToDto(projectName, userFullName);
        }
 
        public static HourEntrySummaryDto ToSummaryDto(this HourEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            return new HourEntrySummaryDto
            {
                Id = entry.Id,
                ProjectName = entry.Project?.Name ?? "N/A",
                Date = entry.Date,
                TotalHours = entry.TotalHours,
                TotalCost = entry.TotalCost,
                Currency = entry.Currency,

                IsPremium = entry.IsPremium,
                PremiumStatus = entry.IsPremium switch
                {
                    false => "Normal",
                    true when entry.PremiumApprovalStatus == Domain.Enums.ApprovalStatus.Approved => "Premium Approuvé",
                    true when entry.PremiumApprovalStatus == Domain.Enums.ApprovalStatus.Rejected => "Premium Refusé",
                    _ => "Premium En Attente"
                },

                AllocationType = entry.AllocationType,
                ProjectType = entry.ProjectType
            };
        }
 
        public static IEnumerable<HourEntryDto> ToDtoList(
            this IEnumerable<HourEntry> entries)
        {
            return entries.Select(e => e.ToDto());
        }
 
        public static IEnumerable<HourEntrySummaryDto> ToSummaryDtoList(
            this IEnumerable<HourEntry> entries)
        {
            return entries.Select(e => e.ToSummaryDto());
        }
    }
}