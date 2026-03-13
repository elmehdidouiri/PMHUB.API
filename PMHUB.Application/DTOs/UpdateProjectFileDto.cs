using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class UpdateProjectFileDto
    {
        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
