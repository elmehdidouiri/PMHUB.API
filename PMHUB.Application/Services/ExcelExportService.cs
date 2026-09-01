using ClosedXML.Excel;
using PMHUB.Application.DTOs;
using PMHUB.Application.IServices;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System;

namespace PMHUB.Application.Services
{
    public class ExcelExportService : IExcelExportService
    {
        private string GetExportsFolder()
        {
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            return folderPath;
        }

        public string GenerateMonthlyExcel(IEnumerable<ProjectExportDto> data, int year, int month)
        {
            string folderPath = GetExportsFolder();
            string fileName = $"Projects_Mois_{month}_{year}.xlsx";
            string filePath = Path.Combine(folderPath, fileName);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add($"Mois_{month}_{year}");

            worksheet.Cell(1, 1).Value = "Projet";
            worksheet.Cell(1, 2).Value = "Phase";
            worksheet.Cell(1, 3).Value = "Estimated Hours";
            worksheet.Cell(1, 4).Value = "Jan Hours";
            worksheet.Cell(1, 5).Value = "Feb Hours";
            worksheet.Cell(1, 6).Value = "Département";
            worksheet.Cell(1, 7).Value = "Sponsor";
            worksheet.Cell(1, 8).Value = "Cost Center";

            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var item in data)
            {
                SetExportCellValue(worksheet.Cell(row, 1), item.Project);
                SetExportCellValue(worksheet.Cell(row, 2), item.Phase);
                SetExportCellValue(worksheet.Cell(row, 3), item.EstimatedHours);
                SetExportCellValue(worksheet.Cell(row, 4), item.TotalBookingHoursJanuary);
                SetExportCellValue(worksheet.Cell(row, 5), item.TotalBookingHoursFebruary);
                SetExportCellValue(worksheet.Cell(row, 6), item.Department);
                SetExportCellValue(worksheet.Cell(row, 7), item.Sponsor);
                SetExportCellValue(worksheet.Cell(row, 8), item.CostCenter);
                row++;
            }

            worksheet.Columns().AdjustToContents();

            workbook.SaveAs(filePath);

             return $"/exports/{fileName}";
        }

        public string GenerateYearlyExcel(IEnumerable<ProjectExportDto> data, int companyYear)
        {
            string folderPath = GetExportsFolder();
            string fileName = $"Projects_Année_{companyYear}.xlsx";
            string filePath = Path.Combine(folderPath, fileName);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add($"Année_{companyYear}");

            worksheet.Cell(1, 1).Value = "Projet";
            worksheet.Cell(1, 2).Value = "Phase";
            worksheet.Cell(1, 3).Value = "Estimated Hours";
            worksheet.Cell(1, 4).Value = "Jan Hours";
            worksheet.Cell(1, 5).Value = "Feb Hours";
            worksheet.Cell(1, 6).Value = "Département";
            worksheet.Cell(1, 7).Value = "Sponsor";
            worksheet.Cell(1, 8).Value = "Cost Center";

            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var item in data)
            {
                SetExportCellValue(worksheet.Cell(row, 1), item.Project);
                SetExportCellValue(worksheet.Cell(row, 2), item.Phase);
                SetExportCellValue(worksheet.Cell(row, 3), item.EstimatedHours);
                SetExportCellValue(worksheet.Cell(row, 4), item.TotalBookingHoursJanuary);
                SetExportCellValue(worksheet.Cell(row, 5), item.TotalBookingHoursFebruary);
                SetExportCellValue(worksheet.Cell(row, 6), item.Department);
                SetExportCellValue(worksheet.Cell(row, 7), item.Sponsor);
                SetExportCellValue(worksheet.Cell(row, 8), item.CostCenter);
                row++;
            }

            worksheet.Columns().AdjustToContents();

            workbook.SaveAs(filePath);

            return $"/exports/{fileName}";
        }

        public string GenerateProjectsExcel(IEnumerable<ProjectSummaryDto> data, string? exportType = null, int? fiscalYear = null)
        {
            var projectData = data.ToList();
            var normalizedExportType = exportType?.Trim().ToLowerInvariant();
            var bookingMonths = ResolveBookingExportMonths(normalizedExportType, fiscalYear).ToList();
            string folderPath = GetExportsFolder();
            string fileName = $"Projects_{ResolveExportFileNameSuffix(normalizedExportType, fiscalYear)}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = Path.Combine(folderPath, fileName);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Projects");

            var headers = BuildProjectExportHeaders(normalizedExportType, fiscalYear, bookingMonths);
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var item in projectData)
            {
                SetExportCellValue(worksheet.Cell(row, 1), item.Name);
                SetExportCellValue(worksheet.Cell(row, 2), item.Status);
                SetExportCellValue(worksheet.Cell(row, 3), item.Phase);
                SetExportCellValue(worksheet.Cell(row, 4), item.ProjectType);
                SetExportCellValue(worksheet.Cell(row, 5), item.StartDate);
                SetExportCellValue(worksheet.Cell(row, 6), item.EndDate);
                SetExportCellValue(worksheet.Cell(row, 7), item.DepartmentName);
                SetExportCellValue(worksheet.Cell(row, 8), item.PlantName);
                SetExportCellValue(worksheet.Cell(row, 9), item.Sponsor);
                SetExportCellValue(worksheet.Cell(row, 10), item.CostCenter);
                SetExportCellValue(worksheet.Cell(row, 11), item.EstimatedHours);
                SetExportCellValue(worksheet.Cell(row, 12), item.ActualHours);

                if (string.Equals(normalizedExportType, "mtd", StringComparison.OrdinalIgnoreCase))
                {
                    SetExportCellValue(worksheet.Cell(row, 13), item.TotalBookingHoursCurrentMonth);
                }
                else if (IsMonthlyFiscalExport(normalizedExportType))
                {
                    var monthlyHours = item.FiscalYtdMonthlyBookingHours
                        .ToDictionary(month => month.MonthStart, month => month.Hours);

                    for (int i = 0; i < bookingMonths.Count; i++)
                    {
                        SetExportCellValue(worksheet.Cell(row, 13 + i), monthlyHours.GetValueOrDefault(bookingMonths[i]));
                    }
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);

            return $"/exports/{fileName}";
        }

        public string GenerateProjectBookingHoursExcel(IEnumerable<ProjectBookingExportDto> data, string periodLabel)
        {
            string folderPath = GetExportsFolder();
            string fileName = $"Projects_BookingHours_{SanitizeFileName(periodLabel)}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = Path.Combine(folderPath, fileName);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Projects Booking Hours");

            var headers = new[]
            {
                "Projects",
                "Phase",
                "Estimated Hours",
                "Departement",
                "Sponsor",
                "Cost Center",
                $"Total Booking Hours {periodLabel}"
            };

            for (int i = 0; i < headers.Length; i++)
                worksheet.Cell(1, i + 1).Value = headers[i];

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var item in data)
            {
                SetExportCellValue(worksheet.Cell(row, 1), item.Project);
                SetExportCellValue(worksheet.Cell(row, 2), item.Phase);
                SetExportCellValue(worksheet.Cell(row, 3), item.EstimatedHours);
                SetExportCellValue(worksheet.Cell(row, 4), item.Department);
                SetExportCellValue(worksheet.Cell(row, 5), item.Sponsor);
                SetExportCellValue(worksheet.Cell(row, 6), item.CostCenter);
                SetExportCellValue(worksheet.Cell(row, 7), item.TotalBookingHours);
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);

            return $"/exports/{fileName}";
        }

        public string GenerateMemberTahExcel(MemberTahDashboardDto data)
        {
            string folderPath = GetExportsFolder();
            string fileName = $"Member_TAH_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = Path.Combine(folderPath, fileName);

            using var workbook = new XLWorkbook();
            AddMemberTahSummarySheet(workbook, data);
            AddMemberTahMonthlySheet(workbook, data);
            AddMemberTahMembersSheet(workbook, data);
            AddMemberTahMemberMonthsSheet(workbook, data);

            workbook.SaveAs(filePath);
            return $"/exports/{fileName}";
        }

        private static void AddMemberTahSummarySheet(XLWorkbook workbook, MemberTahDashboardDto data)
        {
            var worksheet = workbook.Worksheets.Add("Summary");
            ApplyHeaderStyle(worksheet, new[] { "Field", "Value" });

            var rows = new (string Field, object? Value)[]
            {
                ("Period Start", data.Period.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("Period End", data.Period.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("Fiscal Year", data.Period.FiscalYear),
                ("TAH Monthly Hours Target", data.TahMonthlyHoursTarget),
                ("Employee Count", data.Summary.EmployeeCount),
                ("Subcontractor Count", data.Summary.SubcontractorCount),
                ("Average Effectiveness", data.Summary.AverageEffectiveness),
                ("Cumulative TAH Hours", data.Summary.CumulativeTahHours),
                ("Employee TAH Hours", data.Summary.EmployeeTahHours),
                ("Subcontractor TAH Hours", data.Summary.SubcontractorTahHours),
                ("Employee Share %", data.Summary.EmployeeSharePercentage),
                ("Subcontractor Share %", data.Summary.SubcontractorSharePercentage)
            };

            for (int i = 0; i < rows.Length; i++)
            {
                SetExportCellValue(worksheet.Cell(i + 2, 1), rows[i].Field);
                SetExportCellValue(worksheet.Cell(i + 2, 2), rows[i].Value);
            }

            worksheet.Columns().AdjustToContents();
        }

        private static void AddMemberTahMonthlySheet(XLWorkbook workbook, MemberTahDashboardDto data)
        {
            var worksheet = workbook.Worksheets.Add("Monthly Breakdown");
            ApplyHeaderStyle(worksheet, new[]
            {
                "Year",
                "Month",
                "Month Name",
                "Employee Count",
                "Subcontractor Count",
                "Average Effectiveness",
                "TAH Hours",
                "Employee TAH Hours",
                "Subcontractor TAH Hours",
                "Employee Share %",
                "Subcontractor Share %"
            });

            int row = 2;
            foreach (var month in data.MonthlyBreakdown)
            {
                SetExportCellValue(worksheet.Cell(row, 1), month.Year);
                SetExportCellValue(worksheet.Cell(row, 2), month.Month);
                SetExportCellValue(worksheet.Cell(row, 3), month.MonthName);
                SetExportCellValue(worksheet.Cell(row, 4), month.EmployeeCount);
                SetExportCellValue(worksheet.Cell(row, 5), month.SubcontractorCount);
                SetExportCellValue(worksheet.Cell(row, 6), month.AverageEffectiveness);
                SetExportCellValue(worksheet.Cell(row, 7), month.TahHours);
                SetExportCellValue(worksheet.Cell(row, 8), month.EmployeeTahHours);
                SetExportCellValue(worksheet.Cell(row, 9), month.SubcontractorTahHours);
                SetExportCellValue(worksheet.Cell(row, 10), month.EmployeeSharePercentage);
                SetExportCellValue(worksheet.Cell(row, 11), month.SubcontractorSharePercentage);
                row++;
            }

            worksheet.Columns().AdjustToContents();
        }

        private static void AddMemberTahMembersSheet(XLWorkbook workbook, MemberTahDashboardDto data)
        {
            var worksheet = workbook.Worksheets.Add("Members");
            ApplyHeaderStyle(worksheet, new[]
            {
                "Member",
                "Type",
                "Booked Hours",
                "Effectiveness",
                "TAH Hours"
            });

            int row = 2;
            foreach (var member in data.Members)
            {
                SetExportCellValue(worksheet.Cell(row, 1), member.UserName);
                SetExportCellValue(worksheet.Cell(row, 2), member.MemberTypeLabel);
                SetExportCellValue(worksheet.Cell(row, 3), member.BookedHours);
                SetExportCellValue(worksheet.Cell(row, 4), member.Effectiveness);
                SetExportCellValue(worksheet.Cell(row, 5), member.TahHours);
                row++;
            }

            worksheet.Columns().AdjustToContents();
        }

        private static void AddMemberTahMemberMonthsSheet(XLWorkbook workbook, MemberTahDashboardDto data)
        {
            var worksheet = workbook.Worksheets.Add("Member Months");
            ApplyHeaderStyle(worksheet, new[]
            {
                "Member",
                "Type",
                "Year",
                "Month",
                "Month Name",
                "Booked Hours",
                "Effectiveness",
                "TAH Hours"
            });

            int row = 2;
            foreach (var member in data.Members)
            {
                foreach (var month in member.Monthly)
                {
                    SetExportCellValue(worksheet.Cell(row, 1), member.UserName);
                    SetExportCellValue(worksheet.Cell(row, 2), member.MemberTypeLabel);
                    SetExportCellValue(worksheet.Cell(row, 3), month.Year);
                    SetExportCellValue(worksheet.Cell(row, 4), month.Month);
                    SetExportCellValue(worksheet.Cell(row, 5), month.MonthName);
                    SetExportCellValue(worksheet.Cell(row, 6), month.BookedHours);
                    SetExportCellValue(worksheet.Cell(row, 7), month.Effectiveness);
                    SetExportCellValue(worksheet.Cell(row, 8), month.TahHours);
                    row++;
                }
            }

            worksheet.Columns().AdjustToContents();
        }

        private static void ApplyHeaderStyle(IXLWorksheet worksheet, IReadOnlyList<string> headers)
        {
            for (int i = 0; i < headers.Count; i++)
                worksheet.Cell(1, i + 1).Value = headers[i];

            var headerRange = worksheet.Range(1, 1, 1, headers.Count);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Font.FontColor = XLColor.White;
        }

        private static string[] BuildProjectExportHeaders(
            string? exportType,
            int? fiscalYear,
            IReadOnlyCollection<DateTime> bookingMonths)
        {
            var headers = new List<string>
            {
                "Name",
                "Status",
                "Phase",
                "Type",
                "Start Date",
                "End Date",
                "Department",
                "Plant",
                "Sponsor",
                "Cost Center",
                "Est. Hours",
                "Act. Hours"
            };

            if (string.Equals(exportType, "mtd", StringComparison.OrdinalIgnoreCase))
            {
                var currentMonthName = DateTime.Today.ToString("MMMM", CultureInfo.InvariantCulture);
                headers.Add($"Total Booking Hours ({currentMonthName})");
            }
            else if (string.Equals(exportType, "ytd", StringComparison.OrdinalIgnoreCase))
            {
                headers.AddRange(bookingMonths
                    .Select(month => $"Hours {month.ToString("MMM", CultureInfo.InvariantCulture)}"));
            }
            else if (string.Equals(exportType, "fy", StringComparison.OrdinalIgnoreCase))
            {
                var resolvedFiscalYear = fiscalYear ?? GetCurrentFiscalYear();
                headers.AddRange(bookingMonths
                    .Select(month => $"FY {resolvedFiscalYear} Hours {month.ToString("MMM", CultureInfo.InvariantCulture)}"));
            }

            return headers.ToArray();
        }

        private static IEnumerable<DateTime> ResolveBookingExportMonths(string? exportType, int? fiscalYear)
        {
            if (string.Equals(exportType, "ytd", StringComparison.OrdinalIgnoreCase))
                return GetCurrentFiscalMonths();

            if (string.Equals(exportType, "fy", StringComparison.OrdinalIgnoreCase))
                return GetFiscalYearMonths(fiscalYear ?? GetCurrentFiscalYear());

            return Enumerable.Empty<DateTime>();
        }

        private static bool IsMonthlyFiscalExport(string? exportType)
        {
            return string.Equals(exportType, "ytd", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(exportType, "fy", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<DateTime> GetCurrentFiscalMonths()
        {
            var today = DateTime.Today;
            var fiscalYearStart = today.Month < 10
                ? new DateTime(today.Year - 1, 10, 1)
                : new DateTime(today.Year, 10, 1);
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);

            for (var month = fiscalYearStart; month <= currentMonthStart; month = month.AddMonths(1))
            {
                yield return month;
            }
        }

        private static IEnumerable<DateTime> GetFiscalYearMonths(int fiscalYear)
        {
            var fiscalYearStart = new DateTime(fiscalYear - 1, 10, 1);
            var fiscalYearEnd = new DateTime(fiscalYear, 9, 1);

            for (var month = fiscalYearStart; month <= fiscalYearEnd; month = month.AddMonths(1))
            {
                yield return month;
            }
        }

        private static int GetCurrentFiscalYear()
        {
            var today = DateTime.Today;
            return today.Month >= 10 ? today.Year + 1 : today.Year;
        }

        private static string ResolveExportFileNameSuffix(string? exportType, int? fiscalYear)
        {
            return exportType switch
            {
                "mtd" => "MTD",
                "ytd" => "YTD",
                "fy" => $"FY{fiscalYear ?? GetCurrentFiscalYear()}",
                _ => "Standard"
            };
        }

        private static string SanitizeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(value
                .Select(ch => invalidChars.Contains(ch) || char.IsWhiteSpace(ch) ? '_' : ch)
                .ToArray());

            return string.IsNullOrWhiteSpace(sanitized) ? "Period" : sanitized;
        }

        private static void SetExportCellValue(IXLCell cell, object? value)
        {
            if (value is null)
            {
                cell.Value = "-";
                return;
            }

            switch (value)
            {
                case string text:
                    cell.Value = string.IsNullOrWhiteSpace(text) || string.Equals(text.Trim(), "N/A", StringComparison.OrdinalIgnoreCase)
                        ? "-"
                        : text;
                    break;
                case decimal number:
                    cell.Value = number == 0m ? "-" : number;
                    break;
                case int number:
                    cell.Value = number == 0 ? "-" : number;
                    break;
                case double number:
                    cell.Value = number == 0d ? "-" : number;
                    break;
                case DateTime date:
                    cell.Value = date == default ? "-" : date;
                    break;
                default:
                    cell.Value = XLCellValue.FromObject(value);
                    break;
            }
        }
    }
}
