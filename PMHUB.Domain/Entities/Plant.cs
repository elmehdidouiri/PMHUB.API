using System.ComponentModel.DataAnnotations;

namespace PMHUB.Domain.Entities
{
    public class Plant
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Department> Departments { get; set; } = new List<Department>();
    }
}