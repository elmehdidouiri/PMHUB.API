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
        public List<MemberCapacityDto> MemberCapacities { get; set; } = new();
        public CapacityTargetDto CapacityTarget { get; set; } = new();
        public List<MemberPriceDto> MemberPrices { get; set; } = new();
        public PriceTargetDto PriceTarget { get; set; } = new();
    }

    public class MemberCapacityDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public decimal BookedHours { get; set; }
        public decimal Percentage { get; set; }
    }

    public class CapacityTargetDto
    {
        public decimal ActualBookedHours { get; set; }
        public decimal TargetHours { get; set; }
        public decimal RemainingHours { get; set; }
    }

    public class MemberPriceDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public decimal BookedPrice { get; set; }
        public decimal Percentage { get; set; }
    }

    public class PriceTargetDto
    {
        public decimal BookedPrice { get; set; }
        public decimal TargetPrice { get; set; }
        public decimal RemainingPrice { get; set; }
    }
}
