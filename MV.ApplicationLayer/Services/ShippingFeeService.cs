using Microsoft.Extensions.Configuration;
using MV.ApplicationLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class ShippingFeeService : IShippingFeeService
{
    private readonly decimal _freeShippingThreshold;
    private readonly decimal _innerCityFee;
    private readonly decimal _provinceFee;
    private readonly HashSet<string> _innerCityProvinces;

    public ShippingFeeService(IConfiguration configuration)
    {
        var section = configuration.GetSection("Shipping");
        _freeShippingThreshold = section.GetValue<decimal>("FreeShippingThreshold", 500_000);
        _innerCityFee = section.GetValue<decimal>("InnerCityFee", 15_000);
        _provinceFee = section.GetValue<decimal>("ProvinceFee", 30_000);

        var innerCityList = section.GetSection("InnerCityProvinces").Get<List<string>>()
            ?? new List<string> { "Hồ Chí Minh", "Hà Nội", "Ho Chi Minh", "Ha Noi" };

        _innerCityProvinces = new HashSet<string>(innerCityList, StringComparer.OrdinalIgnoreCase);
    }

    public decimal CalculateShippingFee(decimal subtotal, string? province)
    {
        if (subtotal >= _freeShippingThreshold)
            return 0;

        if (!string.IsNullOrWhiteSpace(province) && _innerCityProvinces.Contains(province.Trim()))
            return _innerCityFee;

        return _provinceFee;
    }

    public decimal GetFreeShippingThreshold() => _freeShippingThreshold;
}
