using System.Collections.Generic;

namespace PMHUB.Application.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalProjects { get; set; }
        public double AverageEffectiveness { get; set; }
        public double AverageOtd { get; set; }
        public int DelayedProjects { get; set; }
        public int DoneProjectsBelowTarget { get; set; }
        public int DoneProjectsAboveTarget { get; set; }
        public Dictionary<string, int> ProjectsByPhase { get; set; } = new Dictionary<string, int>();
    }
}
