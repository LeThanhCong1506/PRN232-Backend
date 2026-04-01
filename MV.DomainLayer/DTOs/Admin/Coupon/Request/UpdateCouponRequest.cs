using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Admin.Coupon.Request;

public class UpdateCouponRequest
{
    [Required(ErrorMessage = "Coupon code is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 50 characters")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Discount type is required")]
    [RegularExpression("^(PERCENTAGE|FIXED_AMOUNT)$", ErrorMessage = "Discount type must be PERCENTAGE or FIXED_AMOUNT")]
    public string DiscountType { get; set; } = null!;

    [Required(ErrorMessage = "Discount value is required")]
    [Range(0.01, 100_000_000, ErrorMessage = "Discount value must be greater than 0")]
    public decimal DiscountValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Min order value must be >= 0")]
    public decimal? MinOrderValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Max discount amount must be >= 0")]
    public decimal? MaxDiscountAmount { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Usage limit must be at least 1")]
    public int? UsageLimit { get; set; }
}
