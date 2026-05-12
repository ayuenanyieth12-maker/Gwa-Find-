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

        // GET: /Listing — browse all listings with search & filter
        public async Task<IActionResult> Index(string search, string type, string propertyType, int? minPrice, int? maxPrice, int? bedrooms)
        {
            var listings = _db.Listings
                .Include(l => l.Images)
                .Include(l => l.Owner)
                .Where(l => l.Status == "Active")
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                listings = listings.Where(l => l.Location.Contains(search) || l.District.Contains(search) || l.Title.Contains(search));

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

        // GET: /Listing/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var listing = await _db.Listings
                .Include(l => l.Images)
                .Include(l => l.Owner)
                .Include(l => l.Inquiries)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null) return NotFound();

            return View(listing);
        }

        // GET: /Listing/Create
        [Authorize(Roles = "Owner")]
        public IActionResult Create() => View();

        // POST: /Listing/Create
        [Authorize(Roles = "Owner")]
        [HttpPost]
        public async Task<IActionResult> Create(Listing model, List<IFormFile> images)
        {
            if (!ModelState.IsValid) return View(model);

            model.OwnerId = _userManager.GetUserId(User);
            model.Status = "Pending";
            model.CreatedAt = DateTime.Now;
            model.Amenities = model.Amenities ?? "";

            _db.Listings.Add(model);
            await _db.SaveChangesAsync();

            // save uploaded images
            if (images != null && images.Count > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var image in images.Take(8))
                {
                    if (image.Length > 0)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                        var filePath = Path.Combine(uploadsFolder, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await image.CopyToAsync(stream);

                        _db.ListingImages.Add(new ListingImage
                        {
                            ListingId = model.Id,
                            ImagePath = "/uploads/" + fileName
                        });
                    }
                }
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Listing submitted! It will go live after admin approval.";
            return RedirectToAction("Index", "Dashboard");
        }

        // POST: /Listing/SendInquiry
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendInquiry(int listingId, string message)
        {
            var inquiry = new Inquiry
            {
                ListingId = listingId,
                SeekerId = _userManager.GetUserId(User),
                Message = message,
                SentAt = DateTime.Now
            };
            _db.Inquiries.Add(inquiry);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Inquiry sent successfully!";
            return RedirectToAction("Details", new { id = listingId });
        }

        // POST: /Listing/SaveFavorite
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveFavorite(int listingId)
        {
            var userId = _userManager.GetUserId(User);
            var exists = await _db.Favorites.AnyAsync(f => f.ListingId == listingId && f.SeekerId == userId);

            if (!exists)
            {
                _db.Favorites.Add(new Favorite
                {
                    ListingId = listingId,
                    SeekerId = userId,
                    SavedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = listingId });
        }

        // POST: /Listing/Report
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Report(int listingId, string reason)
        {
            _db.Reports.Add(new Report
            {
                ListingId = listingId,
                ReportedById = _userManager.GetUserId(User),
                Reason = reason,
                ReportedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Listing reported. Our team will review it.";
            return RedirectToAction("Details", new { id = listingId });
        }
    }
}