using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using DTOs;
using Models;


namespace Services
{
    public interface IAdminService
    {
        // ── Profile Picture ───────────────────────────────────
        Task UploadProfilePictureAsync(Guid userId, IFormFile file);
        Task<(byte[] Data, string Name, string Type)?> GetProfilePictureAsync(Guid userId);

        // ── Profile Data ──────────────────────────────────────
        Task<UpdateAdminProfileDto?> GetProfileAsync(Guid userId);
        Task UpdateProfileAsync(Guid userId, UpdateAdminProfileDto dto);

        // ── User CRUD & Stats ─────────────────────────────────
        Task<List<UserDto>> GetAllUsersAsync();
        Task<UserDto?>      GetUserByIdAsync(Guid id);
        Task<List<UserDto>> GetUsersByRoleAsync(string role);
        Task<List<MonthlyStatDto>> GetMonthlyUserStatsAsync();
        Task<bool> UserExistsAsync(Guid userId);
        Task SetUserActiveStateAsync(Guid userId, bool isActive, Guid requesterId);

        //    User roles      //
        Task<List<string>> GetUserRoleNamesAsync(Guid userId);
        Task RemoveRoleFromUserAsync(Guid userId, Guid roleId, Guid requesterId);

        Task AssignRolesAsync(Guid userId, List<string> roles, Guid requesterId);
        Task<List<RoleDto>> GetAllRolesAsync(Guid requesterId);
    
        


        // ── Manage Users ──────────────────────────────────────
        Task CreateUserAsync(RegisterRequest req);
        Task UpdateUserInfoAsync(Guid userId, UpdateUserInfo dto);
        Task ChangePasswordAsync(Guid userId, ChangePasswordRequest req);
        Task DeleteUserAsync(Guid userId);

        // ── Faculties & Courses ───────────────────────────────
        Task<IEnumerable<UpdateFacultyDto>> GetAllFacultiesAsync();
        Task<List<StudentDto>> GetStudentsByFacultyAsync(int facultyId);
        Task<UpdateFacultyDto?> GetFacultyByIdAsync(int facultyId);
        Task CreateNewFacultyAsync(CreateNewFacultyDto dto);
        Task UpdateFacultyAsync(UpdateFacultyDto dto);
        Task<IEnumerable<CourseListDto>> GetAllCoursesAsync();
        Task CreateCourseAsync(AddCourseDto dto);
        Task UpdateCourseAsync(Guid id, UpdateCourseDto dto);
        Task DeleteCourseAsync(Guid id);

        // ── Enrollments ───────────────────────────────────────
        Task<List<EnrollmentWithCourseDto>> GetAllEnrollmentsAsync();
    }
}
