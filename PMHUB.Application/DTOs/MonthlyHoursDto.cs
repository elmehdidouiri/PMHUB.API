using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class MonthlyHoursDto
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;  
        public decimal TotalHours { get; set; }
        public decimal PremiumHours { get; set; }
        public decimal TotalCost { get; set; }
    }
}
