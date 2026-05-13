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

        // GET: Show Create Form
        [Authorize(Roles = "Owner")]
        public IActionResult Create() => View();

        // POST: Create Listing
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
                Console.WriteLine("=== MODEL BINDING ERRORS ===");
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Count > 0)
                    {
                        var errors = string.Join(" | ", state.Value.Errors.Select(e => e.ErrorMessage));
                        Console.WriteLine($"❌ {state.Key}: {errors}");
                    }
                }
                Console.WriteLine("============================");
                return View(model);
            }

            model.OwnerId = _userManager.GetUserId(User)!;
            model.Status = "Pending";
            model.CreatedAt = DateTime.Now;
            model.Amenities = model.Amenities ?? "";

            _db.Listings.Add(model);
            await _db.SaveChangesAsync();

            // Handle Images
            if (images != null && images.Count > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "properties");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var image in images)
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
                            ImagePath = "/uploads/properties/" + fileName
                        });
                    }
                }
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Listing submitted successfully!";
            return RedirectToAction("Index", "Dashboard");
        }

        public async Task<IActionResult> Details(int id)
        {
            var listing = await _db.Listings
                .Include(l => l.Images)
                .Include(l => l.Owner)
                .FirstOrDefaultAsync(l => l.Id == id);

            return listing == null ? NotFound() : View(listing);
        }
    }
}