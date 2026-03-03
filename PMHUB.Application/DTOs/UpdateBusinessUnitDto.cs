

using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class UpdateBusinessUnitDto
    {
        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }
    }
}
