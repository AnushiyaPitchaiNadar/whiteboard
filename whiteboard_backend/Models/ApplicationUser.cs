using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace whiteboard_backend.Models
{
    public class ApplicationUser : IdentityUser
    {

        [Required]
        public string FullName { get; set; }

        // Nullable
        public string? Course { get; set; }

        public ICollection<StudentCourse> StudentCourses { get; set; }
    }
}
