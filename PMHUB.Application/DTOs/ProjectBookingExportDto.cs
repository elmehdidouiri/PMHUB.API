namespace PMHUB.Application.DTOs
{
    public class ProjectBookingExportDto
    {
        public string Project { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public decimal EstimatedHours { get; set; }
        public string Department { get; set; } = string.Empty;
        public string Sponsor { get; set; } = string.Empty;
        public string CostCenter { get; set; } = string.Empty;
        public decimal TotalBookingHours { get; set; }
    }
}
