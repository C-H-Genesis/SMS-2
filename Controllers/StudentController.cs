using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Interfaces;
using Microsoft.AspNetCore.Http;
using DTOs;
using Models;



namespace Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IWebHostEnvironment _env;

        public StudentController(IStudentService studentService, IWebHostEnvironment env)
        {
            _studentService = studentService;
            _env = env;
        }

        private Guid? GetUserId()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            return Guid.TryParse(userIdStr, out var guid) ? guid : null;
        }

        [HttpGet("GetAllCoursesByFaculty")]
        public async Task<IActionResult> GetAllCoursesByFaculty()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var courses = await _studentService.GetAllCoursesByStudentAsync(userId.Value);
            return Ok(courses);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var profile = await _studentService.GetProfileAsync(userId.Value);
            if (profile == null) return NotFound("Student not found.");

            profile.PictureUrl = Url.Action(nameof(GetProfilePicture), "Student", null, Request.Scheme);
            return Ok(profile);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var success = await _studentService.UpdateProfileAsync(userId.Value, dto);
            return success ? Ok("Profile updated.") : BadRequest("Failed to update profile.");
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var success = await _studentService.ChangePasswordAsync(userId.Value, request);
            return success ? Ok("Password changed.") : BadRequest("Incorrect current password or update failed.");
        }

        [HttpPost("registerCourse")]
        public async Task<IActionResult> RegisterCourse([FromBody] RegisterCourseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (!Guid.TryParse(userId, out var userGuid))
                return Unauthorized("User is not authenticated.");

            var result = await _studentService.RegisterCourseAsync(userGuid, dto);

          return result switch
            {
                "Course not found."       => NotFound(new { message = result }),
                "Student not found."      => NotFound(new { message = result }),
                "Already registered."     => BadRequest(new { message = result }),
                var s when s.StartsWith("You have already passed") 
                                        => BadRequest(new { message = s }),
                var s when s.StartsWith("Re‐registered for") 
                                        => Ok(new { message = s }),
                var s                     => Ok(new { message = s })
            };
        }


        [HttpGet("registered-courses")]
        public async Task<IActionResult> GetRegisteredCourses()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await _studentService.GetRegisteredCoursesAsync(userId.Value);
            return Ok(result);
        }

        [HttpGet("Assignments")]
        public async Task<IActionResult> GetAssignmentsForEnrolledCourses()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await _studentService.GetAssignmentsForStudentAsync(userId.Value);
            return Ok(result);
        }

        [HttpPost("profile-picture")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfilePicture([FromForm] UploadProfilePictureDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await _studentService.UploadProfilePictureAsync(userId.Value, dto.File);
            return result ? Ok("Profile picture uploaded.") : NotFound("User not found.");
        }

        [HttpGet("profile-picture")]
        public async Task<IActionResult> GetProfilePicture()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var bytes = await _studentService.GetProfilePictureAsync(userId.Value);
            if (bytes == null) return NotFound("No profile picture found.");

            return File(bytes, "application/octet-stream");
        }

        [HttpPost("PostSubmission")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PostSubmission([FromForm] SubmissionDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await _studentService.PostSubmissionAsync(userId.Value, dto, _env.WebRootPath, Request);
            return result != null ? Ok(result) : BadRequest("Submission failed.");
        }

        [HttpGet("getUploadedFile/{id}")]
        public async Task<IActionResult> GetUploadedFile(Guid id)
        {
            var file = await _studentService.GetUploadedFileByIdAsync(id);
            if (file == null) return NotFound();

            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpGet("results")]
        public async Task<IActionResult> GetMyResults()
        {
            var claim = User.FindFirstValue("UserId");
            if (!Guid.TryParse(claim, out var userId))
                return Unauthorized();

            var results = await _studentService.GetMyResultsAsync(userId);
            return Ok(results);
        }
    }
}
