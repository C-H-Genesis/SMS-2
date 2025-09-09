using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;
using DTOs;

namespace Repositories
{
    public interface IAdminRepository
    {
        // ── Users ──────────────────────────────────────────────
        Task<UserDto?>            GetUserByIdAsync(Guid userId);
        Task<User?> GetEntityByIdAsync(Guid id);
        Task<List<UserDto>> GetAllUserDtosAsync();
        Task<List<UserDto>> GetByRoleAsync(string role);
        Task<List<UserDto>>       GetUsersByRoleAsync(string role);
        Task<User?>            GetByFullNameAsync(string FullName);
        Task UpdateUserProfileAsync(Guid userId, UpdateAdminProfileDto dto);
        Task AddUserAsync(User user); 
        void  UpdateUser(User user);
        void  DeleteUser(User user);
        Task<bool> AnyUserWithIdAsync(Guid userId);
        Task SetUserActiveStateAsync(Guid userId, bool isActive);

        // ── UserRoles ─────────────────────────────────────────
        Task AddUserRoleAsync(UserRole userRole);   
        Task<List<UserRole>> GetUserRolesByUserIdAsync(Guid userId);
        Task<List<string>> GetUserRoleNamesAsync(Guid userId);
        void DeleteUserRolesByUserId(Guid userId);
        Task RemoveUserRoleByIdsAsync(Guid userId, Guid roleId);
        Task<int> GetUserRoleCountAsync(Guid userId);
        Task<int> CountUsersInRoleAsync(string roleName);
        Task<List<string>> GetUserRoleAsync(Guid userId, string roleName);
        Task<bool> IsUserInRoleAsync(Guid userId, string roleName);

        // ── Roles ─────────────────────────────────────────────
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<Role?>            GetRoleByNameAsync(string roleName);
        Task<Role?> GetRoleByIdAsync(Guid roleId);

        // ── Profile Pictures ──────────────────────────────────
        Task<string?> GetPasswordHashByUserIdAsync(Guid userId);
        Task<int> UpdatePasswordHashByUserIdAsync(Guid userId, string newHash);
        Task<UpdateAdminProfileDto?> GetProfileDtoByIdAsync(Guid userId);
        Task<User?>            GetWithPictureAsync(Guid userId);
        Task UpdateProfileAsync(Guid userId, UpdateAdminProfileDto dto);


        // ── Stats ─────────────────────────────────────────────
        Task<List<MonthlyStatDto>> GetMonthlyStatsAsync();

        // ── Faculties ─────────────────────────────────────────
        Task<bool>             FacultyCodeExistsAsync(string code);
        Task<Faculty?> GetFacultyByNameAsync(string facultyName);
        Task AddFacultyAsync(Faculty f);
        Task<List<Student>> GetStudentsByFacultyAsync(int facultyId);
        Task<Faculty?>         GetFacultyByIdAsync(int id);
        Task<IEnumerable<Faculty>> GetAllFacultiesAsync();
        void                   UpdateFaculty(Faculty f);

        // ── Courses ───────────────────────────────────────────
        Task<IEnumerable<CourseListDto>> GetAllCoursesAsync();

        Task<bool>             CourseCodeExistsAsync(string courseCode);
        Task AddCourseAsync(Course c);
        Task<Course?>          GetCourseByIdAsync(Guid id);
        void                   UpdateCourse(Course c);
        void                   RemoveCourse(Course c);

        // ── Enrollments ───────────────────────────────────────
        Task<List<Enrollment>> ListAllEnrollmentsAsync();

        // ── Persistence ───────────────────────────────────────
        Task<int> SaveChangesAsync();
    }
}
