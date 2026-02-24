using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MV.ApplicationLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

/// <summary>
/// Background service chạy mỗi 60 giây để kiểm tra và xử lý các payment SEPAY hết hạn.
/// Khi payment hết hạn → Payment EXPIRED, Order CANCELLED, Stock restored, Coupon restored. (Hoàn lại số lượng, cũng như khuyến mãi)
/// </summary>
public class PaymentExpiryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentExpiryBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    public PaymentExpiryBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PaymentExpiryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("PaymentExpiryBackgroundService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                    await paymentService.ExpireOverduePaymentsAsync();
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PaymentExpiryBackgroundService");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("PaymentExpiryBackgroundService stopped");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("PaymentExpiryBackgroundService cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PaymentExpiryBackgroundService fatal error");
        }
    }
}
