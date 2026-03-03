using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Domain.Entities
{
    public class ProjectResource
    {
        public Guid Id { get; set; }

        // Relation avec le projet
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // Informations sur la ressource
        public string Name { get; set; } = string.Empty;
        public decimal PricePerUnit { get; set; } = 0m;
        public int Quantity { get; set; } = 0;

        public string? CostCenter { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Calcul automatique du coût total
        public decimal TotalCost => PricePerUnit * Quantity;
    }
}
