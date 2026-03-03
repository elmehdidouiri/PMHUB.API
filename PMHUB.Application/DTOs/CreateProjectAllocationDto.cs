using PMHUB.Domain.Enums;
using System;

namespace PMHUB.Application.DTOs
{
    public class CreateProjectAllocationDto
    {
        public Guid UserId { get; set; }
        public decimal AllocatedHours { get; set; }
        public AllocationType AllocationType { get; set; }  
    }
}