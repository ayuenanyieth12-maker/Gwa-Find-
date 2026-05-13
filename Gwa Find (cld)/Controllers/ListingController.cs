using GwaFind.Data;
using GwaFind.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GwaFind.Controllers
{
    public class ListingController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ListingController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
        }

        // GET: Browse all listings
        public async Task<IActionResult> Index(string search, string type, string propertyType, int? minPrice, int? maxPrice, int? bedrooms)
        {
            var listings = _db.Listings
                .Include(l => l.Images)
                .Include(l => l.Owner)
                .Where(l => l.Status == "Active")
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                listings = listings.Where(l => l.Location.Contains(search) ||
                                               l.District.Contains(search) ||
                                               l.Title.Contains(search));
            if (!string.IsNullOrEmpty(type))
                listings = listings.Where(l => l.ListingType == type);
            if (!string.IsNullOrEmpty(propertyType))
                listings = listings.Where(l => l.PropertyType == propertyType);
            if (minPrice.HasValue)
                listings = listings.Where(l => l.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                listings = listings.Where(l => l.Price <= maxPrice.Value);
            if (bedrooms.HasValue)
                listings = listings.Where(l => l.Bedrooms >= bedrooms.Value);

            ViewBag.Search = search;
            ViewBag.Type = type;
            ViewBag.PropertyType = propertyType;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Bedrooms = bedrooms;
            ViewBag.TotalCount = await listings.CountAsync();

            return View(await listings.OrderByDescending(l => l.CreatedAt).ToListAsync());
        }

        // GET: Listing details
        public async Task<IActionResult> Details(int id)
        {
            var listing = await _db.Listings
                .Include(l => l.Images)
                .Include(l => l.Owner)
                .Include(l => l.Inquiries)
                .FirstOrDefaultAsync(l => l.Id == id);

            return listing == null ? NotFound() : View(listing);
        }

        // GET: Create form
        [Authorize(Roles = "Owner")]
        public IActionResult Create() => View();

        // POST: Create listing
        [Authorize(Roles = "Owner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Listing model, List<IFormFile> images)
        {
            ModelState.Remove("OwnerId");
            ModelState.Remove("Owner");
            ModelState.Remove("Status");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("Images");
            ModelState.Remove("Inquiries");
            ModelState.Remove("AreaSqM");

            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                    if (state.Value.Errors.Count > 0)
                        Console.WriteLine($"❌ {state.Key}: {string.Join(" | ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                return View(model);
            }

            model.OwnerId = _userManager.GetUserId(User)!;
            model.Status = "Pending";
            model.CreatedAt = DateTime.Now;
            model.Amenities = model.Amenities ?? "";

            _db.Listings.Add(model);
            await _db.SaveChangesAsync();

            if (images != null && images.Count > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "properties");
                Directory.CreateDirectory(uploadsFolder);
                foreach (var image in images.Where(i => i.Length > 0))
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await image.CopyToAsync(stream);
                    _db.ListingImages.Add(new ListingImage
                    {
                        ListingId = model.Id,
                        ImagePath = "/uploads/properties/" + fileName
                    });
                }
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Listing submitted! It will go live once approved.";
            return RedirectToAction("Index", "Dashboard");
        }

        // POST: Send inquiry
        [Authorize(Roles = "Seeker")]
        [HttpPost]
        public async Task<IActionResult> SendInquiry(int listingId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Message cannot be empty.";
                return RedirectToAction("Details", new { id = listingId });
            }

            _db.Inquiries.Add(new Inquiry
            {
                ListingId = listingId,
                SeekerId = _userManager.GetUserId(User)!,
                Message = message,
                SentAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Inquiry sent successfully!";
            return RedirectToAction("Details", new { id = listingId });
        }

        // POST: Save to favorites
        [Authorize(Roles = "Seeker")]
        [HttpPost]
        public async Task<IActionResult> SaveFavorite(int listingId)
        {
            var userId = _userManager.GetUserId(User)!;

            // Don't save duplicate
            var exists = await _db.Favorites.AnyAsync(f => f.SeekerId == userId && f.ListingId == listingId);
            if (!exists)
            {
                _db.Favorites.Add(new Favorite
                {
                    SeekerId = userId,
                    ListingId = listingId,
                    SavedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = "Saved to favorites!";
            }
            else
            {
                TempData["Success"] = "Already in your saved listings.";
            }

            return RedirectToAction("Details", new { id = listingId });
        }

        // POST: Remove from favorites
        [Authorize(Roles = "Seeker")]
        [HttpPost]
        public async Task<IActionResult> RemoveFavorite(int listingId)
        {
            var userId = _userManager.GetUserId(User)!;
            var fav = await _db.Favorites.FirstOrDefaultAsync(f => f.SeekerId == userId && f.ListingId == listingId);
            if (fav != null)
            {
                _db.Favorites.Remove(fav);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Removed from saved listings.";
            }
            return RedirectToAction("Favorites", "Dashboard");
        }

        // POST: Report listing
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Report(int listingId, string reason)
        {
            _db.Reports.Add(new Report
            {
                ListingId = listingId,
                ReportedById = _userManager.GetUserId(User)!,
                Reason = reason,
                ReportedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Report submitted. Thank you.";
            return RedirectToAction("Details", new { id = listingId });
        }
    }
}