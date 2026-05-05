using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class CreateHourEntryDto
    {
        [Required(ErrorMessage = "Booking type is required.")]
        public BookingType? BookingType { get; set; } // Normal vs Premium

        [Required(ErrorMessage = "Allocation frequency is required.")]
        public AllocationType? AllocationFrequency { get; set; } // Daily vs Weekly

        public DateSelectionMode DateSelectionMode { get; set; } // Single, Multiple, Range

        // For Single/Multiple
        public List<DateTime>? SelectedDates { get; set; }

        // For Weekly Range
        public DateTime? RangeStartDate { get; set; }
        public DateTime? RangeEndDate { get; set; }

        // Core Activity
        public ProjectManagementType Category { get; set; } // DigitalOperation, DigitalSolution, Infrastructure, ProcessSimplification
        public Guid? ProjectId { get; set; } // Filtered by Category

        // Hours Breakdown
        [Range(0, 24)]
        public decimal ExecutionHours { get; set; }
        [Range(0, 24)]
        public decimal TechnicalSupervisionHours { get; set; }
        [Range(0, 24)]
        public decimal ProcessRelatedHours { get; set; }
        [Range(0, 24)]
        public decimal ProjectManagementHours { get; set; }
        [Range(0, 24)]
        public decimal ResearchAndDevHours { get; set; }
        [Range(0, 24)]
        public decimal WorkshopHours { get; set; }
        [Range(0, 24)]
        public decimal OtherActivitiesHours { get; set; }

        // Intern Management
        [Range(0, 24)]
        public decimal InternManagementHours { get; set; }
        public List<InternSupervisionDto>? SupervisedInterns { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public enum DateSelectionMode
    {
        SingleDay,
        MultipleDays,
        WeekRange
    }

    public class InternSupervisionDto
    {
        public Guid InternAllocationId { get; set; }
        public decimal Hours { get; set; }
    }
}
