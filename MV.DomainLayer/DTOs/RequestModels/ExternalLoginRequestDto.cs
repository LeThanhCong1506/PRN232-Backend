using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.RequestModels
{
    public class ExternalLoginRequestDto
    {
        [Required]
        public string Code { get; set; } = null!;

        // RedirectUri is optional: Android requestServerAuthCode() flow passes empty string
        public string? RedirectUri { get; set; }
    }
}
