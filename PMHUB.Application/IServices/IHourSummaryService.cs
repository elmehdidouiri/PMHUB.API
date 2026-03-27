using PMHUB.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.IServices
{
    public interface IHourSummaryService
    {
        Task<IEnumerable<MonthlyHoursDto>> GetMonthlySummary(int year);
        Task<IEnumerable<ProjectHoursDto>> GetProjectSummary(int year);
        Task<IEnumerable<UserHoursDto>> GetUserSummary(int year);
        Task<IEnumerable<ProjectHoursDto>> GetTopProjects(int year, int topCount = 5);
        Task<decimal> GetTotalHoursAsync(int year, int month, Guid? userId = null, Guid? projectId = null);
        Task<Dictionary<string, decimal>> GetBreakdownAsync(int year, int month, Guid? userId = null, Guid? projectId = null);
     
}
}
