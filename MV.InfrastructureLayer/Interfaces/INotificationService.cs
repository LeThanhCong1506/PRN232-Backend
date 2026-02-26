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
    /// Gửi thông báo tới Admin group
    /// </summary>
    Task SendToAdminsAsync(string eventType, object data);
}
