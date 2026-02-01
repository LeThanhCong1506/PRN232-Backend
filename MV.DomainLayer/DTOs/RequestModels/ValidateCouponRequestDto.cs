using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.RequestModels
{
    public class ValidateCouponRequestDto
    {
        [Required(ErrorMessage = "Coupon code is required")]
        [StringLength(50, ErrorMessage = "Coupon code must be max 50 characters")]
        public string CouponCode { get; set; } = null!;
    }
}
