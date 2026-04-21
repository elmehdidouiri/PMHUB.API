using PMHUB.Domain.Enums;
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
        [Required(ErrorMessage = "Criterion type is required.")]
        public StrategicCriterionType Type { get; set; }

        [Required(ErrorMessage = "Score is required.")]
        public StrategicCriterionScore Score { get; set; } = StrategicCriterionScore.Low;

        [MaxLength(500)]
        public string? Comment { get; set; }
    }

}
