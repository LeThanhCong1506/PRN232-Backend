using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces;

public interface INotificationRepository
{
    Task<Notification> CreateAsync(Notification notification);
    Task<List<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task<bool> MarkAllAsReadAsync(int userId);
}
