using System.ComponentModel.DataAnnotations;

namespace whiteboard_backend.Models
{
    public class CourseModel
    {
        [Required]
        public string CourseId { get; set; } // Assuming CourseId is an int

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string CourseName { get; set; }
    }

}
