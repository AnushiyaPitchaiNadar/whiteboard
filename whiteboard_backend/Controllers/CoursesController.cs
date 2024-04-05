using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using whiteboard_backend.Models;
using System.Threading.Tasks;
using whiteboard_backend.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

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
        [Route("myCourses")]
        [Authorize(Roles = UserRoles.STUDENT)]
        public async Task<IActionResult> GetMyCourses()
        {
            var user = await _userManager.FindByEmailAsync(User.Identity.Name); // Get the ID of the currently authenticated student

            var registeredCourses = await _context.StudentCourses
                .Where(sc => sc.UserId == user.Id)
                .Include(sc => sc.Course) // Include the Course data
                .Select(sc => new CourseModel
                {
                    CourseId = sc.CourseId,
                    CourseName = sc.Course.CourseName
                })
                .ToListAsync();

            return Ok(registeredCourses);
        }

        [HttpGet]
        [Route("myCourses/download")]
        [Authorize(Roles = UserRoles.STUDENT)]
        public async Task<IActionResult> DownloadMyCourses()
        {
            var user = await _userManager.FindByEmailAsync(User.Identity.Name); // Get the ID of the currently authenticated student

            var registeredCourses = await _context.StudentCourses
                .Where(sc => sc.UserId == user.Id)
                .Include(sc => sc.Course) // Include the Course data
                .Select(sc => new CourseModel
                {
                    CourseId = sc.CourseId,
                    CourseName = sc.Course.CourseName
                })
                .ToListAsync();

            var stream = new MemoryStream();

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            byte[] bytes;

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("My Courses");
                worksheet.Cells[1, 1].Value = "Course ID";
                worksheet.Cells[1, 2].Value = "Course Name";

                for (int i = 0; i < registeredCourses.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = registeredCourses[i].CourseId;
                    worksheet.Cells[i + 2, 2].Value = registeredCourses[i].CourseName;
                }

                // AutoFit columns for all cells
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                bytes = await package.GetAsByteArrayAsync();
            }
            var file = new FileContentResult(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            file.FileDownloadName = "My Courses.xlsx";
            return file;
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

        [HttpGet]
        [Route("myCourseStudents/download")]
        [Authorize(Roles = UserRoles.PROFESSOR)]
        public async Task<IActionResult> DownloadMyCourseStudents()
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(User.Identity.Name); // Get the ID of the currently authenticated professor

            // Fetch students from the StudentCourse table for these courses
            var studentCourses = await _context.StudentCourses
                                               .Where(sc => sc.CourseId == user.Course)
                                               .Include(sc => sc.User)
                                               .ToListAsync();

            var stream = new MemoryStream();

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            byte[] bytes;

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("Students in " + user.Course);
                worksheet.Cells[1, 1].Value = "Email";
                worksheet.Cells[1, 2].Value = "Full Name";

                for (int i = 0; i < studentCourses.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = studentCourses[i].User.Email;
                    worksheet.Cells[i + 2, 2].Value = studentCourses[i].User.FullName;
                }

                // AutoFit columns for all cells
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                bytes = await package.GetAsByteArrayAsync();
            }
            var file = new FileContentResult(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            file.FileDownloadName = $"Students in {user.Course}.xlsx";
            return file;
        }


    }
}
