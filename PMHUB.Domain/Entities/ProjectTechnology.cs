using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Domain.Entities
{
    public class ProjectTechnology
    {
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public Guid TechnologyId { get; set; }
        public Technology Technology { get; set; } = null!;
    }
}
