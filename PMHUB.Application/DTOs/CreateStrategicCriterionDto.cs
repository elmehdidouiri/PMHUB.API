using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class CreateStrategicCriterionDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Score { get; set; } // 1 = Low, 3 = Medium, 5 = High
    }
}
