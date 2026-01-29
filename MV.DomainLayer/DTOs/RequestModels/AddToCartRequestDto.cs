namespace MV.DomainLayer.DTOs.RequestModels
{
    public class AddToCartRequestDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}