using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PMHUB.Domain.Enums;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class UpdateHourEntryDto
    {
        public BookingType? BookingType { get; set; }

        public CategoryWork? Category { get; set; }

        [Range(0, 24, ErrorMessage = "Hours must be between 0 and 24.")]
        public decimal? TotalHours { get; set; }

        [Range(0, 24, ErrorMessage = "Hours must be between 0 and 24.")]
        public decimal ExecutionHours { get; set; }

        [Range(0, 24, ErrorMessage = "Hours must be between 0 and 24.")]
        public decimal SupervisionHours { get; set; }

        [Range(0, 24, ErrorMessage = "Hours must be between 0 and 24.")]
        public decimal ProcessHours { get; set; }

        [Range(0, 24, ErrorMessage = "Hours must be between 0 and 24.")]
        public decimal ManagementHours { get; set; }

        [Range(0, 24, ErrorMessage = "Hours must be between 0 and 24.")]
        public decimal RAndDHours { get; set; }

        [Range(0, 24, ErrorMessage = "Hours must be between 0 and 24.")]
        public decimal WorkshopHours { get; set; }

        [Range(0, 24, ErrorMessage = "Hours must be between 0 and 24.")]
        public decimal OtherHours { get; set; }

        [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        [MaxLength(1000, ErrorMessage = "Activity note cannot exceed 1000 characters.")]
        public string? ActivityNote { get; set; }
    }
}
