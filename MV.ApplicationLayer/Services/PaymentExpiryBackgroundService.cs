using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MV.ApplicationLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

/// <summary>
/// Hosted Service chạy định kỳ mỗi 2 phút để tự động expire các SEPAY payment
/// đã quá hạn mà không có ai polling (lazy expiry bị bỏ lỡ).
/// Restore stock + coupon cho các order bị cancel.
/// </summary>
public class PaymentExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentExpiryBackgroundService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(2);

    public PaymentExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentExpiryBackgroundService started. Check interval: {Interval}.", CheckInterval);

        // Delay ngắn khi khởi động để tránh chạy ngay khi app đang init
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunExpiryCheckAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentExpiryBackgroundService: Unhandled error during expiry check.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("PaymentExpiryBackgroundService stopped.");
    }

    private async Task RunExpiryCheckAsync()
    {
        // Tạo scope mới cho mỗi lần chạy (IPaymentService là Scoped)
        using var scope = _scopeFactory.CreateScope();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

        _logger.LogDebug("PaymentExpiryBackgroundService: Running overdue payment check at {Time}.", DateTime.UtcNow);
        await paymentService.ExpireOverduePaymentsAsync();
    }
}
