using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PMHUB.Domain.Enums;

namespace PMHUB.Domain.Entities
{
    public class NormalUser : User
    {
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;

        public MemberType MemberType { get; set; } = MemberType.Employee;

        public Guid? RoleId { get; set; }
        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        public Guid? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public Admin? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public ICollection<HourEntry> HourEntries { get; set; } = new List<HourEntry>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    }
}
