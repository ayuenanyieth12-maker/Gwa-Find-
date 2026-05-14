using GwaFind.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GwaFind.Models;

namespace GwaFind.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Notification
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Mark all as read
            var unread = notifications.Where(n => !n.IsRead).ToList();
            unread.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();

            return View(notifications);
        }

        // POST: Mark single as read and redirect to link
        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            var n = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (n != null)
            {
                n.IsRead = true;
                await _db.SaveChangesAsync();
                if (!string.IsNullOrEmpty(n.Link))
                    return Redirect(n.Link);
            }
            return RedirectToAction("Index");
        }

        // POST: Clear all
        [HttpPost]
        public async Task<IActionResult> ClearAll()
        {
            var userId = _userManager.GetUserId(User);
            var all = _db.Notifications.Where(n => n.UserId == userId);
            _db.Notifications.RemoveRange(all);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // GET: unread count (called via fetch for the bell badge)
        public async Task<IActionResult> UnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            var count = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            return Json(count);
        }
    }
}