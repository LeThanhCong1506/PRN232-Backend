namespace MV.ApplicationLayer.Interfaces;

public interface IShippingFeeService
{
    decimal CalculateShippingFee(decimal subtotal, string? province);
    decimal GetFreeShippingThreshold();
}
