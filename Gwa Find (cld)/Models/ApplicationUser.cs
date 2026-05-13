using Microsoft.AspNetCore.Identity;

namespace GwaFind.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; } // "Owner", "Seeker", "Admin"
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? ProfilePicture { get; set; }
    }
}