using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class ProjectExportDto
    {
        [Display(Name = "Projet")]
        public string Project { get; set; } = string.Empty;

        [Display(Name = "Phase")]
        public string Phase { get; set; } = string.Empty;

        [Display(Name = "Estimated Hours")]
        public decimal EstimatedHours { get; set; }

        [Display(Name = "Total Booking Hours January")]
        public decimal TotalBookingHoursJanuary { get; set; }

        [Display(Name = "Total Booking Hours February")]
        public decimal TotalBookingHoursFebruary { get; set; }

        [Display(Name = "Département")]
        public string Department { get; set; } = string.Empty;

        [Display(Name = "Sponsor")]
        public string Sponsor { get; set; } = string.Empty;

        [Display(Name = "Cost Center")]
        public string CostCenter { get; set; } = string.Empty;
    }
}
