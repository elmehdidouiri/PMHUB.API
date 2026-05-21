namespace PMHUB.Domain.Entities
{
    public class ProjectDepartment
    {
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public Guid DepartmentId { get; set; }
        public Department Department { get; set; } = null!;
    }
}
