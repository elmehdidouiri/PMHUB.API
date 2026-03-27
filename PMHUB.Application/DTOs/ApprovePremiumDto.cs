using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class ApprovePremiumDto
    {
        [Required(ErrorMessage = "L'ID de l'entrée est requis")]
        public Guid HourEntryId { get; set; }

        [Required(ErrorMessage = "La décision est requise")]
        public bool IsApproved { get; set; }
    }
}
