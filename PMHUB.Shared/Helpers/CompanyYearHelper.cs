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
            new DateTime(companyYear, 9, 1);

        public static DateTime GetCompanyYearEnd(int companyYear) =>
            new DateTime(companyYear + 1, 8, 31);
    }
}
