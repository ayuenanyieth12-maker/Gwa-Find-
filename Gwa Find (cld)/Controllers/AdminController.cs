using GwaFind.Data;
using GwaFind.Models;
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

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            ViewBag.FullName = user?.FullName ?? user?.Email;
            ViewBag.TotalListings = await _db.Listings.CountAsync();
            ViewBag.PendingListings = await _db.Listings.CountAsync(l => l.Status == "Pending");
            ViewBag.ActiveListings = await _db.Listings.CountAsync(l => l.Status == "Active");
            ViewBag.DeclinedListings = await _db.Listings.CountAsync(l => l.Status == "Declined");
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.TotalOwners = (await _userManager.GetUsersInRoleAsync("Owner")).Count;
            ViewBag.TotalSeekers = (await _userManager.GetUsersInRoleAsync("Seeker")).Count;
            ViewBag.TotalReports = await _db.Reports.CountAsync();

            var pendingListings = await _db.Listings
                .Include(l => l.Owner)
                .Include(l => l.Images)
                .Where(l => l.Status == "Pending")
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var flaggedListings = await _db.Listings
                .Include(l => l.Owner)
                .Include(l => l.Images)
                .Include(l => l.Reports)
                .Where(l => l.Reports.Any())
                .OrderByDescending(l => l.Reports.Count)
                .ToListAsync();

            var allUsers = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            ViewBag.PendingListingsList = pendingListings;
            ViewBag.FlaggedListings = flaggedListings;
            ViewBag.AllUsers = allUsers;

            return View();
        }

        // Approve listing
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var listing = await _db.Listings.FindAsync(id);
            if (listing != null)
            {
                listing.Status = "Active";
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Listing '{listing.Title}' approved.";
            }
            return RedirectToAction("Index");
        }

        // Decline listing
        [HttpPost]
        public async Task<IActionResult> Decline(int id)
        {
            var listing = await _db.Listings.FindAsync(id);
            if (listing != null)
            {
                listing.Status = "Declined";
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Listing '{listing.Title}' declined.";
            }
            return RedirectToAction("Index");
        }

        // Remove listing entirely
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var listing = await _db.Listings.FindAsync(id);
            if (listing != null)
            {
                _db.Listings.Remove(listing);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Listing removed.";
            }
            return RedirectToAction("Index");
        }

        // Delete user
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                TempData["Success"] = $"User '{user.FullName}' deleted.";
            }
            return RedirectToAction("Index");
        }
    }
}