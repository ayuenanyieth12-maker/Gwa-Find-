namespace GwaFind.Models
{
    public class Favorite
    {
        public int Id { get; set; }
        public string SeekerId { get; set; }
        public ApplicationUser Seeker { get; set; }
        public int ListingId { get; set; }
        public Listing Listing { get; set; }
        public DateTime SavedAt { get; set; } = DateTime.Now;
    }
}