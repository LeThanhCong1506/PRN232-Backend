namespace MV.InfrastructureLayer.Interfaces;

/// <summary>
/// Service gửi realtime notification tới client (SignalR + FCM)
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gửi thông báo cart đã thay đổi (cập nhật badge số lượng)
    /// </summary>
    Task SendCartUpdatedAsync(int userId, int totalItems);

    /// <summary>
    /// Gửi thông báo order status đã thay đổi
    /// </summary>
    Task SendOrderStatusChangedAsync(int userId, int orderId, string orderNumber, string newStatus);
    Task NotifyAdminsOrderChangedAsync(int orderId, string orderNumber, string newStatus);

    /// <summary>
    /// Gửi thông báo payment đã được xác nhận thành công
    /// </summary>
    Task SendPaymentConfirmedAsync(int userId, int orderId, string orderNumber, decimal amount);

    /// <summary>
    /// Gửi thông báo payment hết hạn
    /// </summary>
    Task SendPaymentExpiredAsync(int userId, int orderId, string orderNumber);

    /// <summary>
    /// Gửi thông báo tùy chỉnh
    /// </summary>
    Task SendNotificationAsync(int userId, string eventType, object data);

    /// <summary>
    /// Gửi thông báo khi admin thay đổi status warranty claim
    /// </summary>
    Task SendWarrantyClaimStatusChangedAsync(int userId, int claimId, string productName, string newStatus);

    /// <summary>
    /// Gửi thông báo khi admin gửi tin nhắn chat tới customer
    /// </summary>
    Task SendNewChatMessageAsync(int userId, string senderName, string messagePreview);

    /// <summary>
    /// Gửi thông báo khi có đơn hàng mới tới Admin
    /// </summary>
    Task SendAdminNewOrderAsync(int orderId, string orderNumber, decimal totalAmount);

    /// <summary>
    /// Gửi thông báo khách thanh toán thành công tới Admin (VD: Sepay)
    /// </summary>
    Task SendAdminPaymentConfirmedAsync(int orderId, string orderNumber, decimal amount);

    /// <summary>
    /// Gửi thông báo khi có yêu cầu bảo hành mới tới Admin
    /// </summary>
    Task SendAdminNewWarrantyClaimAsync(int claimId, string customerName, string productName);

    /// <summary>
    /// Gửi thông báo khi có yêu cầu trả hàng mới tới Admin
    /// </summary>
    Task SendAdminNewReturnRequestAsync(int requestId, string orderNumber, string customerName);
}
