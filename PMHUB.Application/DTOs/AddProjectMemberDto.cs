using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class AddProjectMemberDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public RoleType ProjectRole { get; set; }
    }
}
