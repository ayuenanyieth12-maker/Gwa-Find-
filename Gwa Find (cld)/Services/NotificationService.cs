using GwaFind.Data;
using GwaFind.Models;

namespace GwaFind.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _db;

        public NotificationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task SendAsync(string userId, string message, string? link = null)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = message,
                Link = link,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }
    }
}