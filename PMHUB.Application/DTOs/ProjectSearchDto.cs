using PMHUB.Domain.Enums;
using System;

namespace PMHUB.Application.DTOs
{
    public class ProjectSearchDto : PaginationQueryDto
    {
        public ProjectStatus? Status { get; set; }
        public ProjectPhase? Phase { get; set; }
        public ProjectType? ProjectType { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? BusinessUnitId { get; set; }
        public Guid? ProjectManagerId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? InternId { get; set; }
    }
}
