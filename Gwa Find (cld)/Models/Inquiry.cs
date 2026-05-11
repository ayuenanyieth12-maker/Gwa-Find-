namespace GwaFind.Models
{
    public class Inquiry
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;
        public string SeekerId { get; set; }
        public ApplicationUser Seeker { get; set; }
        public int ListingId { get; set; }
        public Listing Listing { get; set; }
    }
}