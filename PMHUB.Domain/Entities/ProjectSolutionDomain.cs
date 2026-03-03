using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Domain.Entities
{
    public class ProjectSolutionDomain
    {
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public Guid SolutionDomainId { get; set; }
        public SolutionDomain SolutionDomain { get; set; } = null!;
    }

}
