using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMHUB.Domain.Entities
{
    public class CompanyTargetSettings
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column(TypeName = "decimal(18,2)")]
        public decimal HoursPerDay { get; set; } = 8.5m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AnnualHoursTarget { get; set; } = 2193m;

        public int WorkingDaysPerMonth { get; set; } = 22;

        public int FiscalYearStartMonth { get; set; } = 10;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
