using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.InfrastructureLayer.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly StemDbContext _context;

    public NotificationRepository(StemDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        });
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && n.IsRead == false);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var rowCount = await _context.Notifications
            .Where(n => n.NotificationId == notificationId && n.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return rowCount > 0;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var rowCount = await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead == false)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return rowCount > 0;
    }
}
