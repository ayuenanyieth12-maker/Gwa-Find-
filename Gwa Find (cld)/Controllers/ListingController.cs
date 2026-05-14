using GwaFind.Data;
using GwaFind.Models;
using GwaFind.Services;
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
        private readonly NotificationService _notifications;

        public ListingController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, NotificationService notifications)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
            _notifications = notifications;
        }

        public async Task<IActionResult> Index(string search, string type, string propertyType, int? minPrice, int? maxPrice, int? bedrooms)
        {
            var listings = _db.Listings
                .Include(l => l.Images)
                .Include(l => l.Owner)
                .Where(l => l.Status == "Active")
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                listings = listings.Where(l => l.Location.Contains(search) ||
                                               (l.District != null && l.District.Contains(search)) ||
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

        public async Task<IActionResult> Details(int id)
        {
            var listing = await _db.Listings
                .Include(l => l.Images)
                .Include(l => l.Owner)
                .Include(l => l.Inquiries)
                .FirstOrDefaultAsync(l => l.Id == id);

            return listing == null ? NotFound() : View(listing);
        }

        [Authorize(Roles = "Owner")]
        public IActionResult Create() => View();

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

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var owner = await _userManager.GetUserAsync(User);
            foreach (var admin in admins)
            {
                await _notifications.SendAsync(
                    admin.Id,
                    $"New listing pending approval: \"{model.Title}\" by {owner?.FullName}",
                    "/Admin"
                );
            }

            TempData["Success"] = "Listing submitted! It will go live once approved.";
            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var listing = await _db.Listings
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId);

            if (listing == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            ViewBag.FullName = user?.FullName ?? user?.Email;
            ViewBag.ProfilePicture = user?.ProfilePicture;

            return View(listing);
        }

        [HttpPost]
        [Authorize(Roles = "Owner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Listing model, List<IFormFile> NewImages)
        {
            var userId = _userManager.GetUserId(User);
            var listing = await _db.Listings
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId);

            if (listing == null) return NotFound();

            ModelState.Remove("OwnerId");
            ModelState.Remove("Owner");
            ModelState.Remove("Images");
            ModelState.Remove("Inquiries");
            ModelState.Remove("AreaSqM");
            ModelState.Remove("Status");
            ModelState.Remove("CreatedAt");

            if (!ModelState.IsValid) return View(listing);

            listing.Title = model.Title;
            listing.Description = model.Description;
            listing.Price = model.Price;
            listing.ListingType = model.ListingType;
            listing.PropertyType = model.PropertyType;
            listing.Location = model.Location;
            listing.District = model.District;
            listing.Bedrooms = model.Bedrooms;
            listing.Bathrooms = model.Bathrooms;
            listing.AreaSqM = model.AreaSqM;
            listing.Amenities = model.Amenities ?? "";
            listing.Status = "Pending";

            if (NewImages != null && NewImages.Count > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "properties");
                Directory.CreateDirectory(uploadsFolder);
                foreach (var file in NewImages.Where(f => f.Length > 0))
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                    _db.ListingImages.Add(new ListingImage
                    {
                        ListingId = listing.Id,
                        ImagePath = "/uploads/properties/" + fileName
                    });
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Listing updated! It's pending admin approval again.";
            return RedirectToAction("MyListings", "Dashboard");
        }

        [HttpPost]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteImage(int imageId, int listingId)
        {
            var userId = _userManager.GetUserId(User);
            var image = await _db.ListingImages
                .Include(i => i.Listing)
                .FirstOrDefaultAsync(i => i.Id == imageId && i.Listing.OwnerId == userId);

            if (image != null)
            {
                var fullPath = Path.Combine(_env.WebRootPath, image.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
                _db.ListingImages.Remove(image);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Edit", new { id = listingId });
        }

        [HttpPost]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var listing = await _db.Listings
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId);

            if (listing != null)
            {
                foreach (var img in listing.Images)
                {
                    var fullPath = Path.Combine(_env.WebRootPath, img.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }
                _db.Listings.Remove(listing);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Listing deleted.";
            }

            return RedirectToAction("MyListings", "Dashboard");
        }

        [Authorize(Roles = "Seeker")]
        [HttpPost]
        public async Task<IActionResult> SendInquiry(int listingId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Message cannot be empty.";
                return RedirectToAction("Details", new { id = listingId });
            }

            var userId = _userManager.GetUserId(User)!;

            var inquiry = new Inquiry
            {
                ListingId = listingId,
                SeekerId = userId,
                Message = message,
                SentAt = DateTime.Now
            };
            _db.Inquiries.Add(inquiry);
            await _db.SaveChangesAsync();

            _db.Messages.Add(new Message
            {
                InquiryId = inquiry.Id,
                SenderId = userId,
                Content = message,
                SentAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            var listing = await _db.Listings.FindAsync(listingId);
            var seeker = await _userManager.GetUserAsync(User);
            if (listing != null)
            {
                await _notifications.SendAsync(
                    listing.OwnerId,
                    $"{seeker?.FullName ?? "Someone"} sent an inquiry about \"{listing.Title}\"",
                    $"/Chat/Thread/{inquiry.Id}"
                );
            }

            TempData["Success"] = "Inquiry sent! Continue the conversation in Messages.";
            return RedirectToAction("Thread", "Chat", new { id = inquiry.Id });
        }

        [Authorize(Roles = "Seeker")]
        [HttpPost]
        public async Task<IActionResult> SaveFavorite(int listingId)
        {
            var userId = _userManager.GetUserId(User)!;
            var exists = await _db.Favorites.AnyAsync(f => f.SeekerId == userId && f.ListingId == listingId);
            if (!exists)
            {
                _db.Favorites.Add(new Favorite { SeekerId = userId, ListingId = listingId, SavedAt = DateTime.Now });
                await _db.SaveChangesAsync();
                TempData["Success"] = "Saved to favorites!";
            }
            else
            {
                TempData["Success"] = "Already in your saved listings.";
            }
            return RedirectToAction("Details", new { id = listingId });
        }

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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Report(int listingId, string reason)
        {
            var userId = _userManager.GetUserId(User)!;
            _db.Reports.Add(new Report
            {
                ListingId = listingId,
                ReportedById = userId,
                Reason = reason,
                ReportedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == listingId);
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                await _notifications.SendAsync(
                    admin.Id,
                    $"⚑ \"{listing?.Title}\" was flagged: {reason}",
                    "/Admin"
                );
            }

            TempData["Success"] = "Report submitted. Thank you.";
            return RedirectToAction("Details", new { id = listingId });
        }
    }
}