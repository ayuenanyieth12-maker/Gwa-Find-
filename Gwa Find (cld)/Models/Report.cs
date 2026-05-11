namespace GwaFind.Models
{
    public class Report
    {
        public int Id { get; set; }
        public string Reason { get; set; }
        public DateTime ReportedAt { get; set; } = DateTime.Now;
        public string ReportedById { get; set; }
        public ApplicationUser ReportedBy { get; set; }
        public int ListingId { get; set; }
        public Listing Listing { get; set; }
    }
}