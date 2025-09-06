// Services/NotificationService.cs
using WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Services
{
    public interface INotificationService
    {
        Task SendToUserAsync(int toUserId, int? fromUserId, string title, string? body = null, string? link = null);
        Task SendToUsersAsync(IEnumerable<int> toUserIds, int? fromUserId, string title, string? body = null, string? link = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly Infoeduka2Context _db;
        public NotificationService(Infoeduka2Context db) => _db = db;

        public async Task SendToUserAsync(int toUserId, int? fromUserId, string title, string? body = null, string? link = null)
        {
            _db.Notifications.Add(new Notification
            {
                ToUserId = toUserId,
                FromUserId = fromUserId,
                Title = title,
                Body = body,
                Link = link
            });
            await _db.SaveChangesAsync();
        }

        public async Task SendToUsersAsync(IEnumerable<int> toUserIds, int? fromUserId, string title, string? body = null, string? link = null)
        {
            var rows = toUserIds.Distinct().Select(uid => new Notification
            {
                ToUserId = uid,
                FromUserId = fromUserId,
                Title = title,
                Body = body,
                Link = link
            });
            _db.Notifications.AddRange(rows);
            await _db.SaveChangesAsync();
        }
    }
}
