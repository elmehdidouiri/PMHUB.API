using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class BulkUploadDto
    {
        public List<IFormFile> Files { get; set; } = new();
        public List<int> FileTypes { get; set; } = new();
        public List<string?> Descriptions { get; set; } = new();
    }
}
