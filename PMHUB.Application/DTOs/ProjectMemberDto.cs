using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class ProjectMemberDto
    {
        public Guid ProjectMemberId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public Guid RoleId { get; set; }
        public string? RoleName { get; set; }
         public DateTime JoinedAt { get; set; }
    }
}
