namespace PMHUB.Shared.Helpers
{
    public static class WorkingDaysCalendar
    {
        public static bool IsWeekend(DateTime date) =>
            date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        public static int CountWeekdays(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date;
            if (end < start)
                return 0;

            var count = 0;
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                if (!IsWeekend(date))
                    count++;
            }

            return count;
        }

        public static int CountWeekdaysInMonth(int year, int month)
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return CountWeekdays(start, end);
        }

        public static int CountRemainingWeekdays(DateTime today, DateTime monthStart, DateTime monthEnd)
        {
            today = today.Date;
            monthStart = monthStart.Date;
            monthEnd = monthEnd.Date;

            if (today > monthEnd)
                return 0;

            var from = today < monthStart ? monthStart : today;
            return CountWeekdays(from, monthEnd);
        }
    }
}
