 using System;

namespace PMHUB.Domain.ValueObjects
{
    public class DateRange : IEquatable<DateRange>
    {
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        public int DaysCount => (EndDate.Date - StartDate.Date).Days + 1;

        private DateRange(DateTime startDate, DateTime endDate)
        {
            if (endDate < startDate)
                throw new ArgumentException(
                    "La date de fin ne peut pas être antérieure à la date de début");

            StartDate = startDate.Date;
            EndDate = endDate.Date;
        }

        public static DateRange Create(DateTime startDate, DateTime endDate)
        {
            return new DateRange(startDate, endDate);
        }

        public static DateRange SingleDay(DateTime date)
        {
            return new DateRange(date, date);
        }

        public static DateRange Week(DateTime firstDay)
        {
            var start = firstDay.Date;
            var end = start.AddDays(4); // 5 jours ouvrables
            return new DateRange(start, end);
        }

        public bool Contains(DateTime date)
        {
            var checkDate = date.Date;
            return checkDate >= StartDate && checkDate <= EndDate;
        }

        public bool Overlaps(DateRange other)
        {
            return StartDate <= other.EndDate && other.StartDate <= EndDate;
        }

        public IEnumerable<DateTime> GetWorkDays()
        {
            var current = StartDate;
            while (current <= EndDate)
            {
                if (current.DayOfWeek != DayOfWeek.Saturday &&
                    current.DayOfWeek != DayOfWeek.Sunday)
                {
                    yield return current;
                }
                current = current.AddDays(1);
            }
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as DateRange);
        }

        public bool Equals(DateRange? other)
        {
            return other != null &&
                   StartDate == other.StartDate &&
                   EndDate == other.EndDate;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartDate, EndDate);
        }

        public override string ToString()
        {
            if (StartDate == EndDate)
                return StartDate.ToString("yyyy-MM-dd");

            return $"{StartDate:yyyy-MM-dd} → {EndDate:yyyy-MM-dd} ({DaysCount} jours)";
        }
    }
}