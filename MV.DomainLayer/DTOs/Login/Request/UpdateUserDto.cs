using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Login.Request
{
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        // Ensures a standard format like name@domain.com
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Please enter a valid email format")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required")]
        // Updated for Vietnamese mobile standards: 10 digits starting with 03, 05, 07, 08, 09
        [RegularExpression(@"^(03|05|07|08|09)\d{8}$", ErrorMessage = "Phone number must be a valid 10-digit Vietnamese mobile number")]
        public string? Phone { get; set; }

        public string? Address { get; set; }
        public string? FullName { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
    }
}
