namespace MV.DomainLayer.DTOs.Checkout.Response;

public class PaymentMethodDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public bool IsActive { get; set; }
}
