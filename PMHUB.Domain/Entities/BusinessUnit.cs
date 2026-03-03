
using System.ComponentModel.DataAnnotations;

namespace PMHUB.Domain.Entities
{
    public class BusinessUnit
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Department> Departments { get; set; } = new List<Department>();
        
        public ICollection<ProjectBusinessUnit> ProjectBusinessUnits { get; set; } = new List<ProjectBusinessUnit>();

    }
}