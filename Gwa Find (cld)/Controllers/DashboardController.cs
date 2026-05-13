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

        // Owner dashboard
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var listings = await _db.Listings
                .Include(l => l.Images)
                .Include(l => l.Inquiries)
                .Where(l => l.OwnerId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            ViewBag.TotalListings = listings.Count;
            ViewBag.ActiveListings = listings.Count(l => l.Status == "Active");
            ViewBag.PendingListings = listings.Count(l => l.Status == "Pending");
            ViewBag.TotalInquiries = listings.Sum(l => l.Inquiries?.Count ?? 0);

            return View(listings);
        }

        // Delete listing
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
                TempData["Success"] = "Listing deleted successfully.";
            }

            return RedirectToAction("Index");
        }

        // Seeker favorites
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