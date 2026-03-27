using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class UserHoursDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal PremiumHours { get; set; }
        public decimal TotalCost { get; set; }
    }
}
