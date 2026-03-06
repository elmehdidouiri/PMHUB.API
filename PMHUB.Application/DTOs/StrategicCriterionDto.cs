using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class StrategicCriterionDto
    {
        public Guid Id { get; set; }
        public StrategicCriterionType Type { get; set; }
        public string TypeLabel => Type.ToString();
        public StrategicCriterionScore Score { get; set; }
        public int ScoreValue => (int)Score;
        public string ScoreLabel => Score.ToString();
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
