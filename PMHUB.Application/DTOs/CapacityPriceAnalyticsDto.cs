using System;
using System.Collections.Generic;

namespace PMHUB.Application.DTOs
{
    public class CapacityPriceQueryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal? TargetHoursPerMember { get; set; }
        public decimal HourlyRate { get; set; } = 27m;
    }

    public class CapacityPriceDashboardDto
    {
        /// <summary>
        /// Active user counts by population. Capacity and price figures include
        /// employees and subcontractors; intern figures are exposed by the
        /// dedicated intern-capacity-price endpoint.
        /// </summary>
        public MemberPopulationDto MemberCounts { get; set; } = new();
        public List<MemberCapacityDto> MemberCapacities { get; set; } = new();
        public CapacityTargetDto CapacityTarget { get; set; } = new();
        public List<MemberPriceDto> MemberPrices { get; set; } = new();
        public PriceTargetDto PriceTarget { get; set; } = new();
    }

    public class MemberPopulationDto
    {
        public int EmployeeCount { get; set; }
        public int InternCount { get; set; }
        public int SubcontractorCount { get; set; }
    }

    public class MemberCapacityDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string MemberType { get; set; } = string.Empty;
        public decimal BookedHours { get; set; }
        public decimal TargetHours { get; set; }
        public decimal RemainingHours { get; set; }
        /// <summary>Booked hours minus target hours (negative means below target).</summary>
        public decimal VarianceHours { get; set; }
        public decimal Percentage { get; set; }
    }

    public class CapacityTargetDto
    {
        public decimal ActualBookedHours { get; set; }
        public decimal TargetHours { get; set; }
        public decimal RemainingHours { get; set; }
        public decimal VarianceHours { get; set; }
        public decimal AchievementPercentage { get; set; }
    }

    public class MemberPriceDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string MemberType { get; set; } = string.Empty;
        public decimal BookedPrice { get; set; }
        public decimal TargetPrice { get; set; }
        public decimal RemainingPrice { get; set; }
        /// <summary>Booked price minus target price (negative means below target).</summary>
        public decimal VariancePrice { get; set; }
        public decimal Percentage { get; set; }
    }

    public class PriceTargetDto
    {
        public decimal BookedPrice { get; set; }
        public decimal TargetPrice { get; set; }
        public decimal RemainingPrice { get; set; }
        public decimal VariancePrice { get; set; }
        public decimal AchievementPercentage { get; set; }
    }
}
