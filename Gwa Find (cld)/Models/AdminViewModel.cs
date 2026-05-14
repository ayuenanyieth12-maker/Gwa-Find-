using Microsoft.AspNetCore.Identity;

namespace GwaFind.Models
{
    public class AdminIndexViewModel
    {
        public List<Listing> PendingListings { get; set; } = new();
        public List<Report> FlaggedReports { get; set; } = new();
        public List<ApplicationUser> AllUsers { get; set; } = new();
    }
}