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
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public RoleType ProjectRole { get; set; }
        public string ProjectRoleLabel => ProjectRole.ToString();
        public DateTime JoinedAt { get; set; }
    }
}
