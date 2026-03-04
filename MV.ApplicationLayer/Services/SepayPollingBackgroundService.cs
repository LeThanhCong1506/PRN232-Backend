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

    // Track giao dịch đã match thành công → không cần thử lại
    private readonly HashSet<long> _matchedTransactionIds = new();
    // Track ID cao nhất để phân biệt giao dịch mới (chỉ dùng cho amount matching)
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
        try
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
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SepayPollingBackgroundService");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("SepayPollingBackgroundService stopped");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SepayPollingBackgroundService cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SepayPollingBackgroundService fatal error");
        }
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
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        });

        if (sepayResponse?.Transactions == null || sepayResponse.Transactions.Count == 0)
        {
            _logger.LogDebug("SePay Polling: No transactions found");
            return;
        }

        // Lấy danh sách order PENDING SEPAY từ DB để match
        using var scope = _serviceProvider.CreateScope();
        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

        // Kiểm tra có pending orders không - nếu không có thì skip để tiết kiệm resource
        var pendingOrders = await orderRepo.GetPendingSepayOrdersAsync();
        if (pendingOrders.Count == 0)
        {
            // Không có order PENDING → chỉ cập nhật lastProcessedId rồi skip
            var maxId = sepayResponse.Transactions.Max(t => t.Id);
            if (maxId > _lastProcessedId)
                _lastProcessedId = maxId;
            return;
        }

        _logger.LogInformation(
            "SePay Polling: Found {TxCount} transactions, {OrderCount} pending orders, lastProcessedId={LastId}",
            sepayResponse.Transactions.Count, pendingOrders.Count, _lastProcessedId);

        foreach (var tx in sepayResponse.Transactions)
        {
            // Skip tiền RA hoặc giao dịch đã match thành công
            if (tx.AmountIn <= 0 || _matchedTransactionIds.Contains(tx.Id))
                continue;

            var content = tx.TransactionContent ?? "";
            var paymentRef = ExtractPaymentReference(content);

            if (!string.IsNullOrEmpty(paymentRef))
            {
                // === SEVQR reference found → LUÔN thử match (an toàn vì match chính xác theo mã) ===
                // Đây là fix chính: giao dịch có SEVQR ref sẽ được re-check mỗi poll
                // cho đến khi match thành công, kể cả khi order được tạo SAU giao dịch
                var orderNumber = "ORD" + paymentRef.Substring(5);
                _logger.LogInformation(
                    "SePay Polling: Tx {TxId} has SEVQR ref '{PaymentRef}' → trying OrderNumber='{OrderNumber}'",
                    tx.Id, paymentRef, orderNumber);

                if (await TryCompleteOrder(orderRepo, paymentService, orderNumber, tx))
                {
                    _matchedTransactionIds.Add(tx.Id);
                }
            }
            else if (tx.Id > _lastProcessedId)
            {
                // === Không có STEM ref, CHỈ thử cho giao dịch MỚI (tránh false match bằng amount) ===
                _logger.LogInformation(
                    "SePay Polling: New tx {TxId}, AmountIn={AmountIn}, no STEM ref, trying amount match...",
                    tx.Id, tx.AmountIn);

                if (await TryMatchByAmount(orderRepo, paymentService, tx))
                {
                    _matchedTransactionIds.Add(tx.Id);
                }
            }
        }

        // Cập nhật lastProcessedId (chỉ dùng cho amount matching filter)
        var maxTxId = sepayResponse.Transactions.Max(t => t.Id);
        if (maxTxId > _lastProcessedId)
        {
            _lastProcessedId = maxTxId;
            _logger.LogDebug("SePay Polling: Updated lastProcessedId to {LastId}", _lastProcessedId);
        }

        // Cleanup: chỉ giữ IDs còn trong batch hiện tại (xóa các ID cũ đã ra khỏi top 20)
        _matchedTransactionIds.IntersectWith(sepayResponse.Transactions.Select(t => t.Id));
    }

    /// <summary>
    /// Cập nhật order khi tìm được payment reference trực tiếp.
    /// Returns true nếu match thành công (order đã COMPLETED hoặc vừa được cập nhật).
    /// </summary>
    private async Task<bool> TryCompleteOrder(
        IOrderRepository orderRepo, IPaymentService paymentService,
        string orderNumber, SepayTransactionItem tx)
    {
        var order = await orderRepo.GetOrderByOrderNumberAsync(orderNumber);
        if (order == null) return false;

        var paymentStatus = await orderRepo.GetPaymentStatusByOrderIdAsync(order.OrderId);
        if (paymentStatus == PaymentStatusEnum.COMPLETED.ToString()) return true; // Đã xong
        if (paymentStatus != PaymentStatusEnum.PENDING.ToString()) return false;

        // Kiểm tra số tiền
        if (order.Payment != null && tx.AmountIn < order.Payment.Amount)
        {
            _logger.LogWarning(
                "SePay Polling: Số tiền không khớp. OrderId={OrderId}, Expected={Expected}, Received={Received}",
                order.OrderId, order.Payment.Amount, tx.AmountIn);
            return false;
        }

        _logger.LogInformation(
            "SePay Polling: Match bằng STEM ref! OrderId={OrderId}, OrderNumber={OrderNumber}, Amount={Amount}",
            order.OrderId, orderNumber, tx.AmountIn);

        var result = await paymentService.ProcessSuccessCallbackAsync(orderNumber);
        LogResult(order.OrderId, result);
        return result.Success;
    }

    /// <summary>
    /// Match giao dịch với order PENDING bằng số tiền chính xác.
    /// Dùng khi thanh toán qua SePay Payment Gateway (nội dung không chứa STEM...).
    /// Returns true nếu match thành công.
    /// </summary>
    private async Task<bool> TryMatchByAmount(
        IOrderRepository orderRepo, IPaymentService paymentService,
        SepayTransactionItem tx)
    {
        // Lấy tất cả order PENDING có payment method SEPAY
        var pendingOrders = await orderRepo.GetPendingSepayOrdersAsync();

        _logger.LogInformation("SePay Polling: TryMatchByAmount - Found {Count} pending SEPAY orders for tx amount {Amount}",
            pendingOrders.Count, tx.AmountIn);

        foreach (var order in pendingOrders)
        {
            if (order.Payment == null)
            {
                _logger.LogDebug("SePay Polling: Order {OrderId} has no payment record, skipping", order.OrderId);
                continue;
            }

            // Kiểm tra số tiền khớp chính xác
            if (order.Payment.Amount != tx.AmountIn)
            {
                _logger.LogDebug("SePay Polling: Amount mismatch for Order {OrderId}: expected={Expected}, received={Received}",
                    order.OrderId, order.Payment.Amount, tx.AmountIn);
                continue;
            }

            // Kiểm tra order chưa hết hạn
            if (order.Payment.ExpiredAt.HasValue && order.Payment.ExpiredAt.Value < DateTime.Now)
            {
                _logger.LogDebug("SePay Polling: Order {OrderId} payment expired at {ExpiredAt}, skipping",
                    order.OrderId, order.Payment.ExpiredAt);
                continue;
            }

            _logger.LogInformation(
                "SePay Polling: Match bằng số tiền! OrderId={OrderId}, OrderNumber={OrderNumber}, Amount={Amount}",
                order.OrderId, order.OrderNumber, tx.AmountIn);

            var result = await paymentService.ProcessSuccessCallbackAsync(order.OrderNumber);
            LogResult(order.OrderId, result);

            // Đã match 1 order → dừng (không match giao dịch này với order khác)
            return result.Success;
        }

        return false;
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
    /// Tìm chuỗi "SEVQRxxxxxxxxxxx" trong nội dung chuyển khoản
    /// </summary>
    private static string? ExtractPaymentReference(string content)
    {
        var upperContent = content.ToUpper();
        var index = upperContent.IndexOf("SEVQR");
        if (index < 0) return null;

        var remaining = content.Substring(index);
        if (remaining.Length < 16) return null;

        var reference = remaining.Substring(0, 16).ToUpper();
        var digits = reference.Substring(5);
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
