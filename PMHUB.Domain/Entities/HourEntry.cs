using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class HourEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public Guid? InternAllocationId { get; set; } 
    public InternAllocation? InternAllocation { get; set; }  

    [Required]
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid? TaskId { get; set; }
    public ProjectTask? Task { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Hours { get; set; }

    [Required]
    public AllocationType AllocationType { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}