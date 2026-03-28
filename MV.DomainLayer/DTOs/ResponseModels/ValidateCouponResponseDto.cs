namespace MV.DomainLayer.DTOs.ResponseModels
{
    public class ValidateCouponResponseDto
    {
        public int CouponId { get; set; }
        public string Code { get; set; } = null!;
        public string DiscountType { get; set; } = null!;
        public decimal DiscountValue { get; set; }
        public decimal CalculatedDiscount { get; set; }
        public decimal CartSubtotal { get; set; }
        public decimal NewTotal { get; set; }
        public string Message { get; set; } = null!;
    }
}
