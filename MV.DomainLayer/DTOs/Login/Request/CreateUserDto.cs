using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Login.Request
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = null!;

        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        // Regex for a standard email pattern (e.g., user@domain.com)
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email format is not supported")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required")]
        // Updated Regex for Vietnamese mobile standards (10 digits starting with 03, 05, 07, 08, 09)
        [RegularExpression(@"^(03|05|07|08|09)\d{8}$", ErrorMessage = "Invalid Vietnamese phone number format")]
        public string? Phone { get; set; }

        public string? Address { get; set; }
    }
}
