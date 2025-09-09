using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services;
using Models;

namespace AdminController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _svc;
        private readonly ILogger<AdminController> _logger;
        
        public AdminController(IAdminService svc, ILogger<AdminController> logger)  {
            _svc = svc;
            _logger = logger;
            }

        // ── Profile Picture ───────────────────────────────────
        [HttpPost("profile-picture")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfilePicture([FromForm] UploadProfilePictureDto dto)
        {
            var userIdClaim = User.FindFirstValue("UserId");

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing UserId claim.");

            await _svc.UploadProfilePictureAsync(userId, dto.File);
            return Ok(new { message = "Profile picture uploaded." });
        }


        [HttpGet("profile-picture")]
        public async Task<IActionResult> GetProfilePicture()
        {
            var userIdClaim = User.FindFirstValue("UserId");

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing UserId claim.");

            var maybePic = await _svc.GetProfilePictureAsync(userId);

            if (!maybePic.HasValue)
                return NotFound("No profile picture set.");

            // de‑construct the nullable tuple
            var (data, name, type) = maybePic.Value!;

            // note order: File(contents, contentType, fileDownloadName)
            return File(data, type, name);
        }

        // ── Profile Data ──────────────────────────────────────
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
             var userIdClaim = User.FindFirstValue("UserId");

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing UserId claim.");

            var profile = await _svc.GetProfileAsync(userId);
            if (profile == null) return NotFound("Profile not found.");
            return Ok(profile);
        }

        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateAdminProfileDto dto)
        {
            var userIdClaim = User.FindFirstValue("UserId");

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing UserId claim.");

            await _svc.UpdateProfileAsync(userId, dto);
            return NoContent();
        }

        // ── User CRUD & Stats ─────────────────────────────────
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers() =>
            Ok(await _svc.GetAllUsersAsync());

        [HttpGet("users/{id:guid}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _svc.GetUserByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        [HttpGet("user/{role}")]
        public async Task<IActionResult> GetUsersByRole(string role) =>
            Ok(await _svc.GetUsersByRoleAsync(role));

        
        [HttpGet("GellFacultyStudents/{facultyId}")]
        public async Task<IActionResult> GetStudentsByFacultyAsync(int facultyId)
        {
            var students = await _svc.GetStudentsByFacultyAsync(facultyId);

            if (students == null || !students.Any())
                return NotFound($"No students found in faculty with ID {facultyId}.");

            return Ok(students); 
        }    

        [HttpGet("stats/users/monthly")]
        public async Task<IActionResult> GetMonthlyStats() =>
            Ok(await _svc.GetMonthlyUserStatsAsync());

        // ── Manage Users ──────────────────────────────────────
        [HttpPost("Create-user")]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest req)
        {
            await _svc.CreateUserAsync(req);
            return Ok(new { message = "User created and email sent." });
        }

        [HttpPut("userInfo/{id:guid}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserInfo dto)
        {
            await _svc.UpdateUserInfoAsync(id, dto);
            return NoContent();
        }

        [HttpGet("AllRoles")]
        public async Task<IActionResult> GetAllRoles()
        { 
            var claimValue = User.FindFirstValue("UserId");
            if (!Guid.TryParse(claimValue, out var requesterId))
                return Unauthorized("Invalid or missing UserId claim.");

            var roles = await _svc.GetAllRolesAsync(requesterId);
            return Ok(roles); // returns JSON array of { roleId, roleName }
        }

        [HttpPost("AddRole/{id:guid}")]
        public async Task<IActionResult> AssignRoles(Guid id, [FromBody] List<string> roles)
        {
            var claimValue = User.FindFirstValue("UserId");
            if (!Guid.TryParse(claimValue, out var requesterId))
                return Unauthorized("Invalid or missing UserId claim.");

            try
            {
                // pass requesterId so service can authorize/audit
                await _svc.AssignRolesAsync(id, roles, requesterId);
                return Ok(new { message = "Requested roles assigned (existing roles preserved)." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException knf) { return NotFound(new { message = knf.Message }); }
            catch (InvalidOperationException ioe) { return BadRequest(new { message = ioe.Message }); }
        }


        [HttpGet("GetUsersRoles/{id:guid}")]
        public async Task<IActionResult> GetUserRoles(Guid id)
        {
            var claimValue = User.FindFirstValue("UserId");
            if (!Guid.TryParse(claimValue, out var requesterId))
                return Unauthorized("Invalid or missing UserId claim.");

            try
                {
                    var roles = await _svc.GetUserRoleNamesAsync(id);
                    return Ok(roles);
                }
                catch (KeyNotFoundException)
                {
                    return NotFound(new { message = "User not found." });
                }
        }

        [HttpDelete("{userId:guid}/RemoveUserRole/{roleId:guid}")]
        public async Task<IActionResult> RemoveUserRole(Guid userId, Guid roleId)
        {
            var claimValue = User.FindFirstValue("UserId");
            if (!Guid.TryParse(claimValue, out var requesterId)) return Unauthorized();

            try
            {
                await _svc.RemoveRoleFromUserAsync(userId, roleId, requesterId);
                return NoContent(); // 204
            }
            catch (KeyNotFoundException knf) { return NotFound(new { message = knf.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ioe) { return BadRequest(new { message = ioe.Message }); }
        }

        [HttpPatch("{userId:guid}/active")]
        public async Task<IActionResult> SetActive(Guid userId, [FromBody] bool isActive)
        {
            var claimValue = User.FindFirstValue("UserId");
            if (!Guid.TryParse(claimValue, out var requesterId))
                return Unauthorized("Invalid/missing UserId claim.");

            try
            {
                await _svc.SetUserActiveStateAsync(userId, isActive, requesterId);
                return NoContent();
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (KeyNotFoundException knf) { return NotFound(new { message = knf.Message }); }
            catch (InvalidOperationException ioe) { return BadRequest(new { message = ioe.Message }); }
        }




        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
              // 1) Read the claim
           var claimValue = User.FindFirstValue("UserId");
            if (!Guid.TryParse(claimValue, out var userId))
                return Unauthorized("Invalid or missing UserId claim.");

            Console.WriteLine($"[DEBUG] Claim UserId = '{claimValue}'");
            var exists = await _svc.UserExistsAsync(userId);
            Console.WriteLine($"[DEBUG] Any user with UserId {userId}? {exists}");


                
            try
            {
                await _svc.ChangePasswordAsync(userId, req);
                return Ok(new { message = "Password changed successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "User not found." });
            }
            catch (InvalidOperationException ioe)
            {
                return BadRequest(new { message = ioe.Message });
            }
        }


        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            try
            {
                await _svc.DeleteUserAsync(userId);
                return Ok(new { Message = "User deleted successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "User not found." });
            }
        }

        // ── Faculties & Courses ───────────────────────────────
        [HttpGet("GetAllFaculties")]
        public async Task<IActionResult> GetAllFaculties()
        {
            var faculties = await _svc.GetAllFacultiesAsync();
            return Ok(faculties);
        }


        [HttpGet("faculty/{facultyId}")]
        public async Task<IActionResult> GetFacultyById(int facultyId)
        {
            var faculty = await _svc.GetFacultyByIdAsync(facultyId);
            if (faculty == null)
                return NotFound($"No faculty found with ID: {facultyId}");

            return Ok(faculty);
        }


        [HttpPost("CreateNewFaculty")]
        public async Task<IActionResult> CreateFaculty([FromBody] CreateNewFacultyDto dto)
        {
            await _svc.CreateNewFacultyAsync(dto);
            return Ok(new { message = "Faculty created." });
        }

        [HttpPut("UpdateFaculty")]
        public async Task<IActionResult> UpdateFaculty([FromBody] UpdateFacultyDto dto)
        {
            await _svc.UpdateFacultyAsync(dto);
            return NoContent();
        }

        [HttpGet("courses")]
        public async Task<IActionResult> GetAllCourses()
        {
            var courses = await _svc.GetAllCoursesAsync();
            return Ok(courses);
        }


        [HttpPost("Add-New-Course")]
        public async Task<IActionResult> CreateCourse([FromBody] AddCourseDto dto)
        {
            try
                {
                    await _svc.CreateCourseAsync(dto);
                    return Ok(new { message = "Course created successfully." });
                }
                catch (KeyNotFoundException knf)
                {
                    // Not found either teacher or faculty
                    return NotFound(new { message = knf.Message });
                }
                catch (InvalidOperationException ioe)
                {
                    // Business‐rule failure (e.g. duplicate code)
                    return BadRequest(new { message = ioe.Message });
                }
        }

        [HttpPut("UpdateCourses/{id:guid}")]
        public async Task<IActionResult> UpdateCourse(Guid id, [FromBody] UpdateCourseDto dto)
        {
            try
                {
                    await _svc.UpdateCourseAsync(id, dto);
                    return NoContent();
                }
                catch (KeyNotFoundException knf)
                {
                    // Thrown when teacher or faculty not found
                    return NotFound(new { message = knf.Message });
                }
                catch (InvalidOperationException ioe)
                {
                    // Thrown for business‐rule violations, e.g. duplicate code
                    return BadRequest(new { message = ioe.Message });
                }
        }

        [HttpDelete("delete-course/{id:guid}")]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            await _svc.DeleteCourseAsync(id);
            return Ok(new { message = "Course deleted." });
        }

        // ── Enrollments ───────────────────────────────────────
        [HttpGet("GetAllEnrollments")]
        public async Task<IActionResult> GetAllEnrollments() =>
            Ok(await _svc.GetAllEnrollmentsAsync());
    }
}
