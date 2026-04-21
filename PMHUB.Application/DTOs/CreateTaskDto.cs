using PMHUB.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class CreateTaskDto
    {
        [Required(ErrorMessage = "Task name is required.")]
        [StringLength(150, ErrorMessage = "Task name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public TaskStatuss Status { get; set; } = TaskStatuss.NotStarted;  
    }
}