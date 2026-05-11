namespace GwaFind.Models
{
    public class ListingImage
    {
        public int Id { get; set; }
        public string ImagePath { get; set; }
        public int ListingId { get; set; }
        public Listing Listing { get; set; }
    }
}