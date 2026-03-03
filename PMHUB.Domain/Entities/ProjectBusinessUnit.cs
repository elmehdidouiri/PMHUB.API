using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Domain.Entities
{
    public class ProjectBusinessUnit
    {
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public Guid BusinessUnitId { get; set; }
        public BusinessUnit BusinessUnit { get; set; } = null!;
    }
}
