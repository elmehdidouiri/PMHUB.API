using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class CreateProjectResourceDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public decimal PricePerUnit { get; set; }
        public int Quantity { get; set; }

        public Guid? CostCenterId { get; set; }
    }
}
