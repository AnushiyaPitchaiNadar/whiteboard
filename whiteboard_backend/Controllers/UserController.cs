using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using whiteboard_backend.Models;

namespace whiteboard_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        [Route("liststudents")]
        [Authorize(Roles = UserRoles.ADMIN)] // This ensures only admins can access this route
        public async Task<IActionResult> GetAllStudents()
        {
            // This will hold the list of student details to return
            var studentDetailsList = new List<StudentDetailsModel>();

            // Find all users in the 'Student' role
            var usersInStudentRole = await _userManager.GetUsersInRoleAsync(UserRoles.STUDENT);

            foreach (var user in usersInStudentRole)
            {
                var studentDetails = new StudentDetailsModel
                {
                    Email = user.Email,
                    FullName = user.FullName
                };
                studentDetailsList.Add(studentDetails);
            }

            return Ok(studentDetailsList);
        }

        [HttpGet]
        [Route("listprofessors")]
        [Authorize(Roles = UserRoles.ADMIN)] // Only admins should access this
        public async Task<IActionResult> GetAllProfessors()
        {
            // This will hold the list of professor details to return
            var professorDetailsList = new List<ProfessorDetailsModel>();

            // Find all users in the 'Professor' role
            var usersInProfessorRole = await _userManager.GetUsersInRoleAsync(UserRoles.PROFESSOR);

            foreach (var user in usersInProfessorRole)
            {
                var claims = await _userManager.GetClaimsAsync(user);
                var fullNameClaim = claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "";
                var courseClaim = claims.FirstOrDefault(c => c.Type == "Course")?.Value ?? "";

                var professorDetails = new ProfessorDetailsModel
                {
                    Email = user.Email,
                    FullName = user.FullName,
                    Course = user.Course
                };
                professorDetailsList.Add(professorDetails);
            }

            return Ok(professorDetailsList);
        }

        [HttpDelete("{username}")]
        public async Task<IActionResult> DeleteStudent(string username)
        {
            // Find the user by their email, which is also the username
            var student = await _userManager.FindByEmailAsync(username);
            if (student == null)
            {
                return NotFound(new { message = "Student not found." });
            }

            // Check if the user is in the student role
            var isStudent = await _userManager.IsInRoleAsync(student, UserRoles.STUDENT);
            if (!isStudent)
            {
                // Respond with an error if the user is not a student
                return BadRequest(new { message = "The provided user is not a student." });
            }

            // Perform the deletion
            var result = await _userManager.DeleteAsync(student);
            if (!result.Succeeded)
            {
                // If the deletion fails, send back an error message
                return BadRequest(new { message = "Deletion failed.", errors = result.Errors });
            }

            return Ok(new { message = "Student deleted successfully." });
        }
    }
}
