using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ApplicationDbContext;
using Models;
using DTOs;
using Interfaces;

namespace Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class TeachersController : ControllerBase
    {
        private readonly ITeacherService _teacherService;
        private readonly IWebHostEnvironment _env;

        public TeachersController(ITeacherService teacherService, IWebHostEnvironment env)
        {
            _teacherService = teacherService;
            _env = env;
        }

        private Guid? GetUserId()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            return Guid.TryParse(userId, out var guid) ? guid : null;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            var profile = await _teacherService.GetProfileAsync(teacherId.Value);
            return profile is null ? NotFound() : Ok(profile);
        }

        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            var success = await _teacherService.UpdateProfileAsync(teacherId.Value, dto);
            return success ? Ok("Profile updated.") : NotFound();
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            var success = await _teacherService.ChangePasswordAsync(teacherId.Value, request);
            return success ? Ok("Password changed.") : BadRequest("Current password is incorrect.");
        }

        [HttpGet("GetCourses")]
        public async Task<IActionResult> GetCourses()
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            var courses = await _teacherService.GetCoursesAsync(teacherId.Value);
            return courses.Any() ? Ok(courses) : NotFound("No courses found.");
        }

        [HttpGet("students/{courseCode}")]
        public async Task<IActionResult> GetStudentsByCourse(string courseCode)
        {
            var students = await _teacherService.GetStudentsByCourseAsync(courseCode);
            return Ok(students);
        }

        [HttpPost("uploadAssignmentFile")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAssignmentFile([FromForm] UploadAssignmentDto model)
        {
            var fileResult = await _teacherService.UploadAssignmentFileAsync(model.File, Request, _env.WebRootPath);
            return fileResult == null
                ? BadRequest("File upload failed.")
                : Ok(fileResult);
        }

        [HttpPost("createNewAssignment")]
        public async Task<IActionResult> CreateAssignment([FromBody] AssignmentDto dto)
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Title) ||
                string.IsNullOrWhiteSpace(dto.Description) ||
                dto.CourseId == Guid.Empty)
                return BadRequest("Missing required fields.");

            var assignment = await _teacherService.CreateAssignmentAsync(dto, teacherId.Value);
            return Ok(new { message = "Assignment posted successfully", Id = dto.Id });
        }

        [HttpGet("GetAllAssignments")]
        public async Task<IActionResult> GetAllAssignments()
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            var result = await _teacherService.GetAllAssignmentsAsync(teacherId.Value);
            return Ok(result);
        }

        [HttpGet("AllSubmissions")]
        public async Task<IActionResult> GetAllSubmissions()
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            var result = await _teacherService.GetAllSubmissionsAsync(teacherId.Value);
            return Ok(result);
        }

        [HttpGet("submissions/by-course/{courseId}")]
        public async Task<IActionResult> GetSubmissionsByCourse(Guid courseId)
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            var result = await _teacherService.GetSubmissionsByCourseAsync(teacherId.Value, courseId);
            return Ok(result);
        }

        [HttpGet("Submissions/{id}")]
        public async Task<IActionResult> GetSubmissionById(Guid id)
        {
            var submission = await _teacherService.GetSubmissionByIdAsync(id);
            return submission == null ? NotFound() : Ok(submission);
        }

        [HttpPut("UpdateSubmission/{id:guid}")]
        public async Task<IActionResult> UpdateSubmission(Guid id, [FromBody] SubmissionDto dto)
        {
            var success = await _teacherService.UpdateSubmissionAsync(id, dto);
            return success ? NoContent() : NotFound();
        }

        [HttpDelete("DeleteSubmission/{id}")]
        public async Task<IActionResult> DeleteSubmission(Guid id)
        {
            var success = await _teacherService.DeleteSubmissionAsync(id);
            return success ? NoContent() : NotFound();
        }

        [HttpPost("submissions/{id}/grade")]
        public async Task<IActionResult> GradeSubmission(Guid id, [FromBody] GradeDto dto)
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            var result = await _teacherService.GradeSubmissionAsync(id, dto, teacherId.Value);
            return result.Contains("Graded") ? Ok(new { message = result }) : BadRequest(result);
        }

        [HttpPut("submissions/{id}/grade")]
        public async Task<IActionResult> UpdateGrade(Guid id, [FromBody] GradeDto dto)
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            var result = await _teacherService.UpdateSubmissionGradeAsync(id, dto, teacherId.Value);
            return result.Contains("updated") ? Ok(new { message = result }) : BadRequest(result);
        }

        [HttpGet("downloadUploadedFile/{id}")]
        public async Task<IActionResult> GetUploadedFile(Guid id)
        {
            var file = await _teacherService.GetUploadedFileByIdAsync(id);
            return file == null
                ? NotFound()
                : File(file.Content, file.ContentType, file.FileName);
        }
    }
}




    