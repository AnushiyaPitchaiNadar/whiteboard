using System.ComponentModel.DataAnnotations;

namespace whiteboard_backend.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        public string? FullName { get; set; }

        [EmailAddress]
        [Required(ErrorMessage = "Email is required")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string? Role { get; set; }

        public string? Course { get; set; }
    }
}
