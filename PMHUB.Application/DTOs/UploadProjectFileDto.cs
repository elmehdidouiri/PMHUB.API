using Microsoft.AspNetCore.Http;
using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class UploadProjectFileDto
    {
        [Required(ErrorMessage = "File is required.")]
        public IFormFile File { get; set; } = null!;

        [Required(ErrorMessage = "Document type is required.")]
        public ProjectFileType FileType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
