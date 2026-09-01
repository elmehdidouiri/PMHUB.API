using PMHUB.Shared.Helpers;

namespace PMHUB.Shared.Models
{
    public class CompanyStandards
    {
        public decimal HoursPerDay { get; set; } = 8.31m;
        public decimal AnnualHoursTarget { get; set; } = 2193m;
        public int WorkingDaysPerMonth { get; set; } = 22;
        public int FiscalYearStartMonth { get; set; } = 10;

        public decimal MonthlyHoursTarget => Math.Round(AnnualHoursTarget / 12m, 2);

        public decimal GetDailyHoursForMonth(int year, int month)
        {
            var weekdays = WorkingDaysCalendar.CountWeekdaysInMonth(year, month);
            return weekdays <= 0
                ? 0m
                : Math.Round(MonthlyHoursTarget / weekdays, 2, MidpointRounding.AwayFromZero);
        }

        public decimal GetAvailableHours(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date;
            if (end < start)
                return 0m;

            decimal total = 0m;
            var cursor = new DateTime(start.Year, start.Month, 1);
            var last = new DateTime(end.Year, end.Month, 1);

            while (cursor <= last)
            {
                var monthStart = cursor;
                var monthEnd = cursor.AddMonths(1).AddDays(-1);
                var monthWeekdays = WorkingDaysCalendar.CountWeekdays(monthStart, monthEnd);
                if (monthWeekdays > 0)
                {
                    var rangeStart = start > monthStart ? start : monthStart;
                    var rangeEnd = end < monthEnd ? end : monthEnd;
                    var rangeWeekdays = WorkingDaysCalendar.CountWeekdays(rangeStart, rangeEnd);
                    total += MonthlyHoursTarget * rangeWeekdays / monthWeekdays;
                }

                cursor = cursor.AddMonths(1);
            }

            return Math.Round(total, 2, MidpointRounding.AwayFromZero);
        }

        public static CompanyStandards Default => new()
        {
            HoursPerDay = 8.31m,
            AnnualHoursTarget = 2193m,
            WorkingDaysPerMonth = 22,
            FiscalYearStartMonth = 10
        };
    }
}
