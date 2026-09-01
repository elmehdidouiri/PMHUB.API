namespace PMHUB.Application.DTOs
{
    public class InternCapacityPriceQueryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal? TargetHoursPerIntern { get; set; }
        public decimal HourlyRate { get; set; } = 27m;
    }

    public class InternCapacityPriceDashboardDto
    {
        public int TotalInterns { get; set; }
        public int InternsWithLoggedHours { get; set; }
        public CapacityTargetDto CapacityTarget { get; set; } = new();
        public PriceTargetDto PriceTarget { get; set; } = new();
        public List<InternCapacityPriceDto> Interns { get; set; } = new();
        public List<InternProjectCapacityPriceDto> Projects { get; set; } = new();
    }

    public class InternCapacityPriceDto
    {
        public Guid InternId { get; set; }
        public string InternName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public Guid SupervisorId { get; set; }
        public string SupervisorName { get; set; } = string.Empty;
        public decimal DirectBookedHours { get; set; }
        public decimal SupervisionHours { get; set; }
        public decimal BookedHours { get; set; }
        public decimal TargetHours { get; set; }
        public decimal RemainingHours { get; set; }
        public decimal Percentage { get; set; }
        public decimal BookedPrice { get; set; }
        public decimal TargetPrice { get; set; }
        public decimal RemainingPrice { get; set; }
        public int HourEntryCount { get; set; }
    }

    public class InternProjectCapacityPriceDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public decimal DirectBookedHours { get; set; }
        public decimal SupervisionHours { get; set; }
        public decimal BookedHours { get; set; }
        public decimal BookedPrice { get; set; }
        public int InternCount { get; set; }
        public int HourEntryCount { get; set; }
    }
}
