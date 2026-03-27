using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class CreateHourEntryDto
    {
        [Required(ErrorMessage = "Le projet est requis")]
        public Guid ProjectId { get; set; }

        [Required(ErrorMessage = "Le type d'allocation est requis")]
        public AllocationType AllocationType { get; set; }

        [Required(ErrorMessage = "Le type de projet est requis")]
        public ProjectType ProjectType { get; set; }

        [Required(ErrorMessage = "La date est requise")]
        public DateTime Date { get; set; }

        [Range(0, 24, ErrorMessage = "Les heures doivent être entre 0 et 24")]
        public decimal ExecutionHours { get; set; }

        [Range(0, 24, ErrorMessage = "Les heures doivent être entre 0 et 24")]
        public decimal SupervisionHours { get; set; }

        [Range(0, 24, ErrorMessage = "Les heures doivent être entre 0 et 24")]
        public decimal ProcessHours { get; set; }

        [Range(0, 24, ErrorMessage = "Les heures doivent être entre 0 et 24")]
        public decimal ManagementHours { get; set; }

        [Range(0, 24, ErrorMessage = "Les heures doivent être entre 0 et 24")]
        public decimal RAndDHours { get; set; }

        [Range(0, 24, ErrorMessage = "Les heures doivent être entre 0 et 24")]
        public decimal WorkshopHours { get; set; }

        [Range(0, 24, ErrorMessage = "Les heures doivent être entre 0 et 24")]
        public decimal OtherHours { get; set; }

        [MaxLength(500, ErrorMessage = "Les notes ne peuvent pas dépasser 500 caractères")]
        public string? Notes { get; set; }
    }
}
