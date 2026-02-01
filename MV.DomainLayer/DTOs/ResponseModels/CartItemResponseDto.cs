namespace MV.DomainLayer.DTOs.ResponseModels
{
    public class CartItemResponseDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal ItemTotal { get; set; }
    }
}