using GwaFind.Data;
using GwaFind.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GwaFind.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;

            var listings = await _db.Listings
                .Include(l => l.Images)
                .Include(l => l.Inquiries)
                .Where(l => l.OwnerId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            ViewBag.TotalListings = listings.Count;
            ViewBag.ActiveListings = listings.Count(l => l.Status == "Active");
            ViewBag.PendingListings = listings.Count(l => l.Status == "Pending");
            ViewBag.DeclinedListings = listings.Count(l => l.Status == "Declined");
            ViewBag.TotalInquiries = listings.Sum(l => l.Inquiries?.Count ?? 0);
            ViewBag.FullName = user?.FullName ?? user?.Email;
            ViewBag.ProfilePicture = user?.ProfilePicture;

            return View(listings);
        }

        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> MyListings(string status = "All")
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;

            var query = _db.Listings
                .Include(l => l.Images)
                .Include(l => l.Inquiries)
                .Where(l => l.OwnerId == userId);

            if (status != "All")
                query = query.Where(l => l.Status == status);

            ViewBag.CurrentStatus = status;
            ViewBag.AllCount = await _db.Listings.CountAsync(l => l.OwnerId == userId);
            ViewBag.ActiveCount = await _db.Listings.CountAsync(l => l.OwnerId == userId && l.Status == "Active");
            ViewBag.PendingCount = await _db.Listings.CountAsync(l => l.OwnerId == userId && l.Status == "Pending");
            ViewBag.DeclinedCount = await _db.Listings.CountAsync(l => l.OwnerId == userId && l.Status == "Declined");
            ViewBag.FullName = user?.FullName ?? user?.Email;
            ViewBag.ProfilePicture = user?.ProfilePicture;

            return View(await query.OrderByDescending(l => l.CreatedAt).ToListAsync());
        }

        [Authorize(Roles = "Owner")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId);
            if (listing != null)
            {
                _db.Listings.Remove(listing);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Listing deleted.";
            }
            return RedirectToAction("MyListings");
        }

        [Authorize(Roles = "Seeker")]
        public async Task<IActionResult> Favorites()
        {
            var userId = _userManager.GetUserId(User);
            var favorites = await _db.Favorites
                .Include(f => f.Listing)
                .ThenInclude(l => l.Images)
                .Where(f => f.SeekerId == userId)
                .OrderByDescending(f => f.SavedAt)
                .ToListAsync();
            return View(favorites);
        }
    }
}