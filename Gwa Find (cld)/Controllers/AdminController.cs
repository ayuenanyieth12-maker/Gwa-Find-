using GwaFind.Data;
using GwaFind.Models;
using GwaFind.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GwaFind.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notifications;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, NotificationService notifications)
        {
            _db = db;
            _userManager = userManager;
            _notifications = notifications;
        }

        public async Task<IActionResult> Index()
        {
            var pending = await _db.Listings
                .Include(l => l.Images)
                .Include(l => l.Owner)
                .Where(l => l.Status == "Pending")
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // Get listings that have reports
            var flaggedListingIds = await _db.Reports
                .Select(r => r.ListingId)
                .Distinct()
                .ToListAsync();

            var flagged = await _db.Listings
                .Include(l => l.Images)
                .Include(l => l.Owner)
                .Include(l => l.Reports)
                .Where(l => flaggedListingIds.Contains(l.Id))
                .ToListAsync();

            var users = await _userManager.Users.ToListAsync();

            ViewBag.PendingListingsList = pending;
            ViewBag.FlaggedListings = flagged;
            ViewBag.AllUsers = users;

            ViewBag.PendingListings = pending.Count;
            ViewBag.TotalListings = await _db.Listings.CountAsync();
            ViewBag.ActiveListings = await _db.Listings.CountAsync(l => l.Status == "Active");
            ViewBag.TotalReports = await _db.Reports.CountAsync();
            ViewBag.TotalUsers = users.Count;

            var ownerIds = (await _userManager.GetUsersInRoleAsync("Owner")).Select(u => u.Id).ToHashSet();
            var seekerIds = (await _userManager.GetUsersInRoleAsync("Seeker")).Select(u => u.Id).ToHashSet();
            ViewBag.OwnerCount = ownerIds.Count;
            ViewBag.SeekerCount = seekerIds.Count;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var listing = await _db.Listings.FindAsync(id);
            if (listing != null)
            {
                listing.Status = "Active";
                await _db.SaveChangesAsync();
                await _notifications.SendAsync(
                    listing.OwnerId,
                    $"✅ Your listing \"{listing.Title}\" has been approved and is now live!",
                    $"/Listing/Details/{listing.Id}"
                );
                TempData["Success"] = "Listing approved.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Decline(int id)
        {
            var listing = await _db.Listings.FindAsync(id);
            if (listing != null)
            {
                listing.Status = "Declined";
                await _db.SaveChangesAsync();
                await _notifications.SendAsync(
                    listing.OwnerId,
                    $"❌ Your listing \"{listing.Title}\" was declined by admin.",
                    "/Dashboard/MyListings"
                );
                TempData["Success"] = "Listing declined.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveListing(int id)
        {
            var listing = await _db.Listings
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing != null)
            {
                _db.Listings.Remove(listing);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Listing removed.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                {
                    TempData["Error"] = "Cannot delete admin accounts.";
                    return RedirectToAction("Index");
                }
                await _userManager.DeleteAsync(user);
                TempData["Success"] = "User deleted.";
            }
            return RedirectToAction("Index");
        }
    }
}