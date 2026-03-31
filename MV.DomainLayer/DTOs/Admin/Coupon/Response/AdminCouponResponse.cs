namespace MV.DomainLayer.DTOs.Admin.Coupon.Response;

public class AdminCouponResponse
{
    public int CouponId { get; set; }
    public string Code { get; set; } = null!;
    public string DiscountType { get; set; } = null!;   // "PERCENTAGE" or "FIXED_AMOUNT"
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int OrderCount { get; set; }
    public bool IsActive { get; set; }  // StartDate <= Now <= EndDate && (no limit or UsedCount < UsageLimit)
}
