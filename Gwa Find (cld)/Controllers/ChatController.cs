using GwaFind.Data;
using GwaFind.Models;
using GwaFind.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GwaFind.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notifications;

        public ChatController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, NotificationService notifications)
        {
            _db = db;
            _userManager = userManager;
            _notifications = notifications;
        }

        // GET: /Chat/Thread/5  (5 = inquiryId)
        public async Task<IActionResult> Thread(int id)
        {
            var userId = _userManager.GetUserId(User);

            var inquiry = await _db.Inquiries
                .Include(i => i.Listing).ThenInclude(l => l.Owner)
                .Include(i => i.Seeker)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inquiry == null) return NotFound();

            // Only the seeker or the listing owner can view this chat
            bool isSeeker = inquiry.SeekerId == userId;
            bool isOwner = inquiry.Listing.OwnerId == userId;
            if (!isSeeker && !isOwner) return Forbid();

            var messages = await _db.Messages
                .Include(m => m.Sender)
                .Where(m => m.InquiryId == id)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // Mark messages from the other person as read
            var unread = messages.Where(m => m.SenderId != userId && !m.IsRead).ToList();
            unread.ForEach(m => m.IsRead = true);
            await _db.SaveChangesAsync();

            ViewBag.InquiryId = id;
            ViewBag.CurrentUserId = userId;
            ViewBag.Inquiry = inquiry;
            ViewBag.IsOwner = isOwner;

            return View(messages);
        }

        // POST: Send a message
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int inquiryId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("Thread", new { id = inquiryId });

            var userId = _userManager.GetUserId(User)!;

            var inquiry = await _db.Inquiries
                .Include(i => i.Listing).ThenInclude(l => l.Owner)
                .Include(i => i.Seeker)
                .FirstOrDefaultAsync(i => i.Id == inquiryId);

            if (inquiry == null) return NotFound();

            bool isSeeker = inquiry.SeekerId == userId;
            bool isOwner = inquiry.Listing.OwnerId == userId;
            if (!isSeeker && !isOwner) return Forbid();

            _db.Messages.Add(new Message
            {
                InquiryId = inquiryId,
                SenderId = userId,
                Content = content,
                SentAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            // Notify the other person
            var recipientId = isOwner ? inquiry.SeekerId : inquiry.Listing.OwnerId;
            var senderName = (await _userManager.GetUserAsync(User))?.FullName ?? "Someone";
            await _notifications.SendAsync(
                recipientId,
                $"{senderName} sent you a message about \"{inquiry.Listing.Title}\"",
                $"/Chat/Thread/{inquiryId}"
            );

            return RedirectToAction("Thread", new { id = inquiryId });
        }

        // GET: /Chat — list all chat threads for the current user
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // Get all inquiries this user is part of (as seeker or as owner)
            var inquiries = await _db.Inquiries
                .Include(i => i.Listing).ThenInclude(l => l.Images)
                .Include(i => i.Listing).ThenInclude(l => l.Owner)
                .Include(i => i.Seeker)
                .Where(i => i.SeekerId == userId || i.Listing.OwnerId == userId)
                .OrderByDescending(i => i.SentAt)
                .ToListAsync();

            // For each inquiry, get unread message count
            var inquiryIds = inquiries.Select(i => i.Id).ToList();
            var unreadCounts = await _db.Messages
                .Where(m => inquiryIds.Contains(m.InquiryId) && m.SenderId != userId && !m.IsRead)
                .GroupBy(m => m.InquiryId)
                .Select(g => new { InquiryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.InquiryId, g => g.Count);

            ViewBag.UnreadCounts = unreadCounts;
            ViewBag.CurrentUserId = userId;

            return View(inquiries);
        }
    }
}