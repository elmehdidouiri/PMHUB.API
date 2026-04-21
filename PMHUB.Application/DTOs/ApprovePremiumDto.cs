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
        [Required(ErrorMessage = "Hour entry ID is required.")]
        public Guid HourEntryId { get; set; }

        [Required(ErrorMessage = "Approval decision is required.")]
        public bool IsApproved { get; set; }
    }
}
