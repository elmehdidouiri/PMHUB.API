using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class ProjectResourceDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal PricePerUnit { get; set; }
        public int Quantity { get; set; }
        public decimal TotalCost { get; set; }
        public string? CostCenter { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
