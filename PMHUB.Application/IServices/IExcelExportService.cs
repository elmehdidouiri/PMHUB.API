using PMHUB.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.IServices
{
    public interface IExcelExportService
    {
        string GenerateMonthlyExcel(IEnumerable<ProjectExportDto> data, int year, int month);

        string GenerateYearlyExcel(IEnumerable<ProjectExportDto> data, int companyYear);
        string GenerateProjectsExcel(IEnumerable<ProjectSummaryDto> data, string? exportType = null, int? fiscalYear = null);
    }
}
