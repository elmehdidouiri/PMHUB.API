using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Shared.Models
{
    public class CompanyStandards
    {
        public decimal HoursPerDay { get; set; } = 8.5m;
        public decimal AnnualHoursTarget { get; set; } = 2193m;
        public int WorkingDaysPerMonth { get; set; } = 22;
        public int FiscalYearStartMonth { get; set; } = 10;

        public decimal MonthlyHoursTarget => Math.Round(AnnualHoursTarget / 12m, 2);

        public static CompanyStandards Default => new()
        {
            HoursPerDay = 8.5m,
            AnnualHoursTarget = 2193m,
            WorkingDaysPerMonth = 22,
            FiscalYearStartMonth = 10
        };
    }
}
