using ClosedXML.Excel;
using PMHUB.Application.DTOs;
using PMHUB.Application.IServices;
using System.Collections.Generic;
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
                worksheet.Cell(row, 1).Value = item.Project;
                worksheet.Cell(row, 2).Value = item.Phase;
                worksheet.Cell(row, 3).Value = item.EstimatedHours;
                worksheet.Cell(row, 4).Value = item.TotalBookingHoursJanuary;
                worksheet.Cell(row, 5).Value = item.TotalBookingHoursFebruary;
                worksheet.Cell(row, 6).Value = item.Department;
                worksheet.Cell(row, 7).Value = item.Sponsor;
                worksheet.Cell(row, 8).Value = item.CostCenter;
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
                worksheet.Cell(row, 1).Value = item.Project;
                worksheet.Cell(row, 2).Value = item.Phase;
                worksheet.Cell(row, 3).Value = item.EstimatedHours;
                worksheet.Cell(row, 4).Value = item.TotalBookingHoursJanuary;
                worksheet.Cell(row, 5).Value = item.TotalBookingHoursFebruary;
                worksheet.Cell(row, 6).Value = item.Department;
                worksheet.Cell(row, 7).Value = item.Sponsor;
                worksheet.Cell(row, 8).Value = item.CostCenter;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            workbook.SaveAs(filePath);

            return $"/exports/{fileName}";
        }

        public string GenerateProjectsExcel(IEnumerable<ProjectSummaryDto> data)
        {
            string folderPath = GetExportsFolder();
            string fileName = $"Projects_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = Path.Combine(folderPath, fileName);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Projects");

            var headers = new string[] { "Name", "Status", "Phase", "Type", "Start Date", "End Date", "Budget", "Department", "Plant", "Sponsor", "Est. Hours", "Act. Hours" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.Name;
                worksheet.Cell(row, 2).Value = item.Status;
                worksheet.Cell(row, 3).Value = item.Phase;
                worksheet.Cell(row, 4).Value = item.ProjectType;
                worksheet.Cell(row, 5).Value = item.StartDate;
                worksheet.Cell(row, 6).Value = item.EndDate;
                worksheet.Cell(row, 7).Value = item.Budget;
                worksheet.Cell(row, 8).Value = item.DepartmentName;
                worksheet.Cell(row, 9).Value = item.PlantName;
                worksheet.Cell(row, 10).Value = item.Sponsor;
                worksheet.Cell(row, 11).Value = item.EstimatedHours;
                worksheet.Cell(row, 12).Value = item.ActualHours;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);

            return $"/exports/{fileName}";
        }
    }
}