using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Order.Request;

public class CreateOrderRequest
{
    [Required(ErrorMessage = "Payment method is required")]
    public string PaymentMethod { get; set; } = null!;

    public string? CouponCode { get; set; }

    [Required(ErrorMessage = "Customer name is required")]
    public string CustomerName { get; set; } = null!;

    [Required(ErrorMessage = "Customer email is required")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string CustomerEmail { get; set; } = null!;

    [Required(ErrorMessage = "Customer phone is required")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải đúng 10 chữ số")]
    public string CustomerPhone { get; set; } = null!;

    [Required(ErrorMessage = "Province is required")]
    public string Province { get; set; } = null!;

    [Required(ErrorMessage = "District is required")]
    public string District { get; set; } = null!;

    [Required(ErrorMessage = "Ward is required")]
    public string Ward { get; set; } = null!;

    [Required(ErrorMessage = "Street address is required")]
    public string StreetAddress { get; set; } = null!;

    public string? Notes { get; set; }
}
