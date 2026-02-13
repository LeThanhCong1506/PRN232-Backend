namespace MV.DomainLayer.DTOs.Checkout.Response;

public class ValidateCheckoutErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public List<StockErrorDto>? Errors { get; set; }
}
