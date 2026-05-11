namespace GwaFind.Models
{
    public class Listing
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ListingType { get; set; } // "Rent" or "Sale"
        public string PropertyType { get; set; } // "Apartment", "House", etc.
        public string Location { get; set; }
        public string District { get; set; }
        public decimal Price { get; set; }
        public string PricePeriod { get; set; } // "Per month", "Fixed"
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public double AreaSqM { get; set; }
        public string ContactPhone { get; set; }
        public string PreferredContact { get; set; }
        public string Amenities { get; set; } // comma separated
        public string Status { get; set; } = "Pending"; // "Active", "Pending", "Taken"
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string OwnerId { get; set; }
        public ApplicationUser Owner { get; set; }
        public List<ListingImage> Images { get; set; }
        public List<Inquiry> Inquiries { get; set; }
    }
}