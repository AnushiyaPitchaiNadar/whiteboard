using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using whiteboard_backend.Models;
using System.Threading.Tasks;
using whiteboard_backend.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace whiteboard_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CoursesController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [Route("add")]
        [Authorize(Roles = UserRoles.ADMIN)] // Ensure that only admins can access this endpoint
        public async Task<IActionResult> AddCourse([FromBody] CourseModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if a course with the same CourseId already exists
            var existingCourse = await _context.Courses.FindAsync(model.CourseId);
            if (existingCourse != null)
            {
                return BadRequest(new { message = "A course with the given CourseId already exists." });
            }

            var course = new Course
            {
                CourseId = model.CourseId,
                CourseName = model.CourseName
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course added successfully." });
        }


        [HttpPost]
        [Route("registerCourse")]
        [Authorize(Roles = UserRoles.STUDENT)] // Ensure that only students can access this endpoint
        public async Task<IActionResult> RegisterCourse([FromBody] CourseRegistrationModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound(new { Message = "Student not found." });
            }

            var course = await _context.Courses.FindAsync(model.CourseId);
            if (course == null)
            {
                return BadRequest(new { Message = "You have chosen an invalid course. Please check the course ID." });
            }

            // Check if the student is already registered for the course
            var existingRegistration = await _context.StudentCourses
                .FirstOrDefaultAsync(sc => sc.UserId == user.Id && sc.CourseId == model.CourseId);
            if (existingRegistration != null)
            {
                return BadRequest(new { Message = "Student is already registered for this course." });
            }

            // Register the student for the course
            var studentCourse = new StudentCourse { UserId = user.Id, CourseId = model.CourseId };
            _context.StudentCourses.Add(studentCourse);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Student registered for course successfully." });
        }

        [HttpGet]
        [Route("myCourseStudents")]
        [Authorize(Roles = UserRoles.PROFESSOR)]
        public async Task<IActionResult> GetMyCourseStudents()
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(User.Identity.Name); // Get the ID of the currently authenticated professor

            // Fetch students from the StudentCourse table for these courses
            var studentCourses = await _context.StudentCourses
                                               .Where(sc => sc.CourseId == user.Course)
                                               .Include(sc => sc.User)
                                               .ToListAsync();

            var studentDetails = studentCourses.Select(sc => new StudentDetailsModel
            {
                Email = sc.User.Email,
                FullName = sc.User.FullName
            }).ToList();

            return Ok(studentDetails);
        }


    }
}
