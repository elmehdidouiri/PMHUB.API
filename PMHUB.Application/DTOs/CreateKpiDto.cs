using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class CreateKpiDto
    {
        [Required(ErrorMessage = "KPI name is required.")]
        public string Name { get; set; } = string.Empty;

        public decimal TargetValue { get; set; } = 0;
        public decimal CurrentValue { get; set; } = 0;
        public DateTime? EstimatedDueDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public decimal EstimatedHours { get; set; } = 0;
        public decimal ActualHours { get; set; } = 0;
        public string? Description { get; set; }
    }
}
