using System.ComponentModel.DataAnnotations;

namespace GwaFind.Models
{
    public class RegisterViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required, MinLength(8)]
        public string Password { get; set; }

        [Required, Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; } = "Seeker"; // default
    }

    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}