using System.ComponentModel.DataAnnotations;

namespace GwaFind.Models
{
    public class Listing
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string ListingType { get; set; } = "Rent";

        [Required(ErrorMessage = "Property type is required")]
        public string PropertyType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        public string? District { get; set; }   // Optional

        [Required]
        [Range(10000, double.MaxValue, ErrorMessage = "Price must be greater than 10,000 UGX")]
        public decimal Price { get; set; }

        public string PricePeriod { get; set; } = "Month";

        public int Bedrooms { get; set; } = 0;
        public int Bathrooms { get; set; } = 1;
        public double? AreaSqM { get; set; }

        [Required(ErrorMessage = "Contact phone is required")]
        public string ContactPhone { get; set; } = string.Empty;

        public string PreferredContact { get; set; } = "WhatsApp";

        public string? Amenities { get; set; }

        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        public ApplicationUser? Owner { get; set; }
        public List<ListingImage> Images { get; set; } = new();
        public List<Inquiry> Inquiries { get; set; } = new();
    }
}