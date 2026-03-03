using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Domain.Entities
{
    public class StrategicCriterion
    {
        public Guid Id { get; set; }

        // Relation avec le projet
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // Type et valeur
        public StrategicCriterionType Type { get; set; }
        public int Score { get; set; } = 1; // Low=1, Medium=3, High=5

        // Optionnel : commentaire / justification
        public string? Comment { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
