using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.RequestModels
{
    public class ExternalLoginRequestDto
    {
        [Required]
        public string Code { get; set; } = null!;

        [Required]
        public string RedirectUri { get; set; } = null!;
    }
}
