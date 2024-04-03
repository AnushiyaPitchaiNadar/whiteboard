using System.ComponentModel.DataAnnotations;

namespace whiteboard_backend.Models
{
    public class Course
    {
        [Key]
        public string CourseId { get; set; }
        public string CourseName { get; set; }

        // many-to-many
        public ICollection<ApplicationUser> Students { get; set; } = new List<ApplicationUser>();
        public ICollection<StudentCourse> StudentCourses { get; set; }
    }
}
