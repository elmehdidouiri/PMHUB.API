using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Shared.Helpers
{
    public static class CompanyYearHelper
    {
        public static DateTime GetCompanyYearStart(int companyYear) =>
            new DateTime(companyYear - 1, 10, 1);

        public static DateTime GetCompanyYearEnd(int companyYear) =>
            new DateTime(companyYear, 9, 30);

        public static int GetCurrentCompanyYear(DateTime? today = null)
        {
            var currentDate = today?.Date ?? DateTime.Today;
            return currentDate.Month >= 10 ? currentDate.Year + 1 : currentDate.Year;
        }

        public static int ResolveCompanyYear(int? companyYear, DateTime? today = null)
        {
            var currentDate = today?.Date ?? DateTime.Today;

            if (!companyYear.HasValue)
            {
                return GetCurrentCompanyYear(currentDate);
            }

            return companyYear.Value;
        }
    }
}
