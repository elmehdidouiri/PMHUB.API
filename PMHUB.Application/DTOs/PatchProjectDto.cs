using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class PatchProjectDto
    {
        public ProjectStatus? Status { get; set; }
        public ProcessStatus? ProcessStatus { get; set; }
        public int? ProgressPercentage { get; set; }
        public string? CurrentState { get; set; }
        public string? Roadblocks { get; set; }
        public string? NextSteps { get; set; }
        public string? Enhancements { get; set; }
        public DateTime? EstimatedDueDate { get; set; }
        public decimal? Budget { get; set; }
        public decimal? CostSaving { get; set; }
        public string? Sponsor { get; set; }
        public Guid? ProjectManagerId { get; set; }
        public string? CodeSourceLink { get; set; }
        public string? SolutionLink { get; set; }
        public string? ServerHostName { get; set; }
    }
}
