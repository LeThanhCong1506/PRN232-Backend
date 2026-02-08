using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Order.Request;

public class CreateOrderRequest
{
    [Required(ErrorMessage = "Payment method is required.")]
    [StringLength(50, ErrorMessage = "Payment method name is too long.")]
    public string PaymentMethod { get; set; } = null!;

    [StringLength(20, ErrorMessage = "Coupon code cannot exceed 20 characters.")]
    public string? CouponCode { get; set; }

    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string CustomerName { get; set; } = null!;

    [Required(ErrorMessage = "Customer email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(255)]
    public string CustomerEmail { get; set; } = null!;

    [Required(ErrorMessage = "Customer phone is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Phone number must be between 10 to 15 digits.")]
    public string CustomerPhone { get; set; } = null!;

    [Required(ErrorMessage = "Province is required.")]
    public string Province { get; set; } = null!;

    [Required(ErrorMessage = "District is required.")]
    public string District { get; set; } = null!;

    [Required(ErrorMessage = "Ward is required.")]
    public string Ward { get; set; } = null!;

    [Required(ErrorMessage = "Street address is required.")]
    [StringLength(200, ErrorMessage = "Address is too long.")]
    public string StreetAddress { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}
