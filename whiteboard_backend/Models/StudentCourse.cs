namespace whiteboard_backend.Models
{
    public class StudentCourse
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string CourseId { get; set; }
        public Course Course { get; set; }
    }

}
