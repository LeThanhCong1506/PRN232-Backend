using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.Enums;
using MV.InfrastructureLayer.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MV.ApplicationLayer.Services;

/// <summary>
/// Background service polling SePay API mỗi 8 giây để kiểm tra giao dịch mới.
/// Khi phát hiện giao dịch khớp với order PENDING → tự động cập nhật COMPLETED.
/// Giải pháp thay thế webhook khi chạy trên localhost.
/// </summary>
public class SepayPollingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SepayPollingBackgroundService> _logger;
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(8); // Thời gian check

    // Lưu ID giao dịch đã xử lý để không xử lý lại
    private long _lastProcessedId = 0;

    public SepayPollingBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<SepayPollingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var apiToken = _configuration["SePay:ApiToken"];
        var apiBaseUrl = _configuration["SePay:ApiBaseUrl"] ?? "https://my.sepay.vn/";
        var accountNumber = _configuration["SePay:AccountNumber"];

        if (string.IsNullOrEmpty(apiToken))
        {
            _logger.LogWarning("SePay ApiToken is not configured. SepayPollingService is not working.");
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiToken);

        _logger.LogInformation("SepayPollingBackgroundService started - polling mỗi {Interval}s", _interval.TotalSeconds);

        // Đợi 10 giây cho app khởi động xong
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollTransactionsAsync(apiBaseUrl, accountNumber, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SepayPollingBackgroundService");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("SepayPollingBackgroundService stopped");
    }

    private async Task PollTransactionsAsync(string apiBaseUrl, string? accountNumber, CancellationToken ct)
    {
        // Gọi API SePay lấy giao dịch gần đây (20 giao dịch mới nhất)
        var url = $"{apiBaseUrl.TrimEnd('/')}/userapi/transactions/list?limit=20";
        if (!string.IsNullOrEmpty(accountNumber))
        {
            url += $"&account_number={accountNumber}";
        }

        var response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("The SePay API returns HTTP {StatusCode}.", response.StatusCode);
            return;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var sepayResponse = JsonSerializer.Deserialize<SepayTransactionListResponse>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        });

        if (sepayResponse?.Transactions == null || sepayResponse.Transactions.Count == 0)
            return;

        // Lấy danh sách order PENDING SEPAY từ DB để match
        using var scope = _serviceProvider.CreateScope();
        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        var sepayRepo = scope.ServiceProvider.GetRequiredService<ISepayRepository>();

        foreach (var tx in sepayResponse.Transactions)
        {
            // Chỉ xử lý giao dịch mới (ID lớn hơn giao dịch đã xử lý cuối cùng)
            if (tx.Id <= _lastProcessedId)
                continue;

            // Chỉ xử lý tiền VÀO (amount_in > 0)
            if (tx.AmountIn <= 0)
                continue;

            // === Cách 1: Tìm "STEM..." trong nội dung (khi user quét QR trực tiếp) ===
            var content = tx.TransactionContent ?? "";
            var paymentRef = ExtractPaymentReference(content);

            if (!string.IsNullOrEmpty(paymentRef))
            {
                // Tìm được STEM... → match trực tiếp
                var orderNumber = "ORD" + paymentRef.Substring(4);
                await TryCompleteOrder(orderRepo, paymentService, orderNumber, tx);
                continue;
            }

            // === Cách 2: Match bằng số tiền (khi user thanh toán qua Payment Gateway) ===
            // Tìm order PENDING có amount khớp chính xác với giao dịch
            await TryMatchByAmount(orderRepo, paymentService, tx);
        }

        // Cập nhật lastProcessedId = max ID trong batch
        var maxId = sepayResponse.Transactions.Max(t => t.Id);
        if (maxId > _lastProcessedId)
        {
            _lastProcessedId = maxId;
        }
    }

    /// <summary>
    /// Cập nhật order khi tìm được payment reference trực tiếp
    /// </summary>
    private async Task TryCompleteOrder(
        IOrderRepository orderRepo, IPaymentService paymentService,
        string orderNumber, SepayTransactionItem tx)
    {
        var order = await orderRepo.GetOrderByOrderNumberAsync(orderNumber);
        if (order == null) return;

        var paymentStatus = await orderRepo.GetPaymentStatusByOrderIdAsync(order.OrderId);
        if (paymentStatus != PaymentStatusEnum.PENDING.ToString()) return;

        // Kiểm tra số tiền
        if (order.Payment != null && tx.AmountIn < order.Payment.Amount)
        {
            _logger.LogWarning(
                "SePay Polling: Số tiền không khớp. OrderId={OrderId}, Expected={Expected}, Received={Received}",
                order.OrderId, order.Payment.Amount, tx.AmountIn);
            return;
        }

        _logger.LogInformation(
            "SePay Polling: Match bằng STEM ref! OrderId={OrderId}, OrderNumber={OrderNumber}, Amount={Amount}",
            order.OrderId, orderNumber, tx.AmountIn);

        var result = await paymentService.ProcessSuccessCallbackAsync(orderNumber);
        LogResult(order.OrderId, result);
    }

    /// <summary>
    /// Match giao dịch với order PENDING bằng số tiền chính xác.
    /// Dùng khi thanh toán qua SePay Payment Gateway (nội dung không chứa STEM...).
    /// </summary>
    private async Task TryMatchByAmount(
        IOrderRepository orderRepo, IPaymentService paymentService,
        SepayTransactionItem tx)
    {
        // Lấy tất cả order PENDING có payment method SEPAY
        var pendingOrders = await orderRepo.GetPendingSepayOrdersAsync();

        foreach (var order in pendingOrders)
        {
            if (order.Payment == null) continue;

            // Kiểm tra số tiền khớp chính xác
            if (order.Payment.Amount != tx.AmountIn) continue;

            // Kiểm tra order chưa hết hạn
            if (order.Payment.ExpiredAt.HasValue && order.Payment.ExpiredAt.Value < DateTime.Now)
                continue;

            _logger.LogInformation(
                "SePay Polling: Match bằng số tiền! OrderId={OrderId}, OrderNumber={OrderNumber}, Amount={Amount}",
                order.OrderId, order.OrderNumber, tx.AmountIn);

            var result = await paymentService.ProcessSuccessCallbackAsync(order.OrderNumber);
            LogResult(order.OrderId, result);

            // Đã match 1 order → dừng (không match giao dịch này với order khác)
            break;
        }
    }

    private void LogResult(int orderId, DomainLayer.DTOs.ResponseModels.ApiResponse<DomainLayer.DTOs.Payment.Response.PaymentStatusResponse> result)
    {
        if (result.Success)
        {
            _logger.LogInformation("SePay Polling: Đã cập nhật OrderId={OrderId} → COMPLETED", orderId);
        }
        else
        {
            _logger.LogWarning("SePay Polling: Không thể cập nhật OrderId={OrderId}: {Message}", orderId, result.Message);
        }
    }

    /// <summary>
    /// Tìm chuỗi "STEMxxxxxxxxxxx" trong nội dung chuyển khoản
    /// </summary>
    private static string? ExtractPaymentReference(string content)
    {
        var upperContent = content.ToUpper();
        var index = upperContent.IndexOf("STEM");
        if (index < 0) return null;

        var remaining = content.Substring(index);
        if (remaining.Length < 15) return null;

        var reference = remaining.Substring(0, 15).ToUpper();
        var digits = reference.Substring(4);
        if (!digits.All(char.IsDigit)) return null;

        return reference;
    }

    public override void Dispose()
    {
        _httpClient.Dispose();
        base.Dispose();
    }
}

// DTO cho response từ SePay API (field names dùng JsonPropertyName vì API trả snake_case)
public class SepayTransactionListResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("messages")]
    public SepayMessages? Messages { get; set; }

    [JsonPropertyName("transactions")]
    public List<SepayTransactionItem> Transactions { get; set; } = new();
}

public class SepayMessages
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

public class SepayTransactionItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("bank_brand_name")]
    public string? BankBrandName { get; set; }

    [JsonPropertyName("account_number")]
    public string? AccountNumber { get; set; }

    [JsonPropertyName("transaction_date")]
    public string? TransactionDate { get; set; }

    [JsonPropertyName("amount_in")]
    public decimal AmountIn { get; set; }

    [JsonPropertyName("amount_out")]
    public decimal AmountOut { get; set; }

    [JsonPropertyName("accumulated")]
    public decimal Accumulated { get; set; }

    [JsonPropertyName("transaction_content")]
    public string? TransactionContent { get; set; }

    [JsonPropertyName("reference_number")]
    public string? ReferenceNumber { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("sub_account")]
    public string? SubAccount { get; set; }

    [JsonPropertyName("bank_account_id")]
    public int? BankAccountId { get; set; }
}
