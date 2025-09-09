using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;
using ApplicationDbContext;
using Repositories;
using DTOs;



namespace Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly SMSDbContext _ctx;
        public AdminRepository(SMSDbContext ctx) => _ctx = ctx;

        // ── Users ──────────────────────────────────────────────
        public async Task<UserDto?> GetUserByIdAsync(Guid userId)
        {
            var user = await _ctx.Users.FindAsync(userId);
            if (user == null) return null;

            string? regNumber = null;

            if (user is Student student)
                regNumber = student.RegNumber;

            return new UserDto
            {
                UserId      = user.UserId,
                FullName    = user.FullName,
                Username    = user.Username,
                Email       = user.Email!,
                PhoneNumber = user.PhoneNumber!,
                UserType    = user.UserType!,
                RegNumber   = regNumber,
                IsActive    = user.IsActive
            };
        }


        public async Task<User?> GetEntityByIdAsync(Guid id)
        {
            return await _ctx.Users.FirstOrDefaultAsync(u => u.UserId == id);
        }    

        // AdminRepository.cs
        // AdminRepository.cs

        public async Task<List<UserDto>> GetAllUserDtosAsync()
        {
            var students = await _ctx.Set<Student>()
                .Select(s => new { s.UserId, s.RegNumber })
                .ToListAsync();

            var users = await _ctx.Users
                .Select(u => new UserDto
                {
                    UserId      = u.UserId,
                    FullName    = u.FullName,
                    Username    = u.Username,
                    Email       = u.Email!,
                    PhoneNumber = u.PhoneNumber!,
                    UserType    = u.UserType!,
                    RegNumber   = null, // Set below
                    IsActive    = u.IsActive
                })
                .ToListAsync();

            foreach (var user in users)
            {
                user.RegNumber = students
                    .FirstOrDefault(s => s.UserId == user.UserId)?.RegNumber;
            }

            return users;
        }


       public async Task<List<UserDto>> GetUsersByRoleAsync(string role)
        {
            var students = await _ctx.Set<Student>()
                .Where(s => s.UserType == role)
                .Select(s => new { s.UserId, s.RegNumber })
                .ToListAsync();

            var users = await _ctx.Users
                .Where(u => u.UserType == role)
                .Select(u => new UserDto
                {
                    UserId      = u.UserId,
                    FullName    = u.FullName,
                    Username    = u.Username,
                    Email       = u.Email!,
                    PhoneNumber = u.PhoneNumber!,
                    UserType    = u.UserType!,
                    RegNumber   = null, // populated below if user is student
                    IsActive    = u.IsActive
                })
                .ToListAsync();

            foreach (var user in users)
            {
                user.RegNumber = students.FirstOrDefault(s => s.UserId == user.UserId)?.RegNumber;
            }

            return users;
        }


        public async Task<User?> GetByFullNameAsync(string FullName) =>
            await _ctx.Users.SingleOrDefaultAsync(u => u.FullName == FullName);

        public async Task<bool> AnyUserWithIdAsync(Guid userId) =>
            await _ctx.Users.AnyAsync(u => u.UserId == userId);    

        public async Task AddUserAsync(User user) =>
            await _ctx.Users.AddAsync(user);

        public void UpdateUser(User user) =>
            _ctx.Users.Update(user);

        public void DeleteUser(User user) =>
            _ctx.Users.Remove(user);

        public async Task UpdateUserProfileAsync(Guid userId, UpdateAdminProfileDto dto)
        {
            // Create a stub with all required properties populated
            var user = new User
            {
                UserId       = userId,
                Username     = string.Empty,  // satisfy 'required'
                FullName     = string.Empty,
                PasswordHash = string.Empty   // satisfy 'required'
            };

            _ctx.Users.Attach(user);

            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                user.Username = dto.Username.Trim();
                _ctx.Entry(user).Property(u => u.Username).IsModified = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                user.FullName = dto.FullName.Trim();
                _ctx.Entry(user).Property(u => u.FullName).IsModified = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                user.Email = dto.Email.Trim();
                _ctx.Entry(user).Property(u => u.Email).IsModified = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                user.PhoneNumber = dto.PhoneNumber.Trim();
                _ctx.Entry(user).Property(u => u.PhoneNumber).IsModified = true;
            }

            await _ctx.SaveChangesAsync();
        }

        public async Task SetUserActiveStateAsync(Guid userId, bool isActive)
        {
            var user = await _ctx.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            user.IsActive = isActive;
            // DO NOT call SaveChanges here if your service will call SaveChangesAsync()
        }

    

        // ── UserRoles ─────────────────────────────────────────
        public async Task AddUserRoleAsync(UserRole userRole) =>
            await _ctx.UserRoles.AddAsync(userRole);

        public async Task<List<UserRole>> GetUserRolesByUserIdAsync(Guid userId) =>
            await _ctx.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();

        public Task<List<string>> GetUserRoleNamesAsync(Guid userId) =>
            (from ur in _ctx.UserRoles.AsNoTracking()
            join r in _ctx.Roles.AsNoTracking() on ur.RoleId equals r.RoleId
            where ur.UserId == userId
            select r.RoleName)
            .ToListAsync();

         // When u want to delete one User role at time //
        public async Task RemoveUserRoleByIdsAsync(Guid userId, Guid roleId)
        {
            var ur = await _ctx.UserRoles.FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId);
            if (ur != null) _ctx.UserRoles.Remove(ur);
            // don't call SaveChanges here
        }

        public async Task<Role?> GetRoleByIdAsync(Guid roleId) =>
            await _ctx.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);

       
            
         // Delete all user roles //
        public void DeleteUserRolesByUserId(Guid userId)
        {
            var userRoles = _ctx.UserRoles.Where(ur => ur.UserId == userId);
            _ctx.UserRoles.RemoveRange(userRoles);
        }

        public Task<int> GetUserRoleCountAsync(Guid userId)
        {
            return _ctx.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.RoleId)   // distinct role ids just in case
                    .Distinct()
                    .CountAsync();
        }

        public async Task<int> CountUsersInRoleAsync(string roleName)
        {
           var role = await _ctx.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role == null) return 0;
            return await _ctx.UserRoles.CountAsync(ur => ur.RoleId == role.RoleId);
        }

         public async Task<List<string>> GetUserRoleAsync(Guid userId, string roleName)
        {
            return await _ctx.UserRoles
                            .Where(ur => ur.UserId == userId)
                            .Include(ur => ur.Role)
                            .Select(ur => ur.Role.RoleName)
                            .ToListAsync();
        }

    

        // ── Roles ─────────────────────────────────────────────
        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            return await _ctx.Roles
                .OrderBy(r => r.RoleName)
                .Select(r => new RoleDto
                {
                    RoleId = r.RoleId,     // adjust property names if different
                    RoleName = r.RoleName
                })
                .ToListAsync();
        }
        
        public async Task<Role?> GetRoleByNameAsync(string roleName) =>
            await _ctx.Roles.SingleOrDefaultAsync(r => r.RoleName == roleName);

        public async Task<List<UserDto>> GetByRoleAsync(string role)
        {
            var students = await _ctx.Set<Student>()
                .Where(s => s.UserType == role)
                .Select(s => new { s.UserId, s.RegNumber })
                .ToListAsync();

            var users = await _ctx.Users
                .Where(u => u.UserType == role)
                .Select(u => new UserDto
                {
                    UserId      = u.UserId,
                    FullName    = u.FullName,
                    Username    = u.Username,
                    Email       = u.Email!,
                    PhoneNumber = u.PhoneNumber!,
                    UserType    = u.UserType!,
                    RegNumber   = null,
                    IsActive    = u.IsActive
                })
                .ToListAsync();

            foreach (var user in users)
            {
                user.RegNumber = students.FirstOrDefault(s => s.UserId == user.UserId)?.RegNumber;
            }

            return users;
        }    

        // ── Profile & Pictures ──────────────────────────────────

              //  Update Password  //

        public async Task<string?> GetPasswordHashByUserIdAsync(Guid userId)
        {
            return await _ctx.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.PasswordHash)
                .FirstOrDefaultAsync(); 
        }

        // Update only the hash column—no blob loaded
        public async Task<int> UpdatePasswordHashByUserIdAsync(Guid userId, string newHash)
        {
            return await _ctx.Users
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(b => 
                    b.SetProperty(u => u.PasswordHash, _ => newHash)
                );
        }
        public async Task<UpdateAdminProfileDto?> GetProfileDtoByIdAsync(Guid userId)
        {
            return await _ctx.Users
                .AsNoTracking()                     // no change‑tracking overhead
                .Where(u => u.UserId == userId)     // filter in SQL
                .Select(u => new UpdateAdminProfileDto
                {
                    FullName    = u.FullName,
                    Username    = u.Username,
                    Email       = u.Email!,
                    PhoneNumber = u.PhoneNumber!
                })
                .FirstOrDefaultAsync();             // single SQL round‑trip
        }

        public async Task UpdateProfileAsync(Guid userId, UpdateAdminProfileDto dto)
        {
            var users = _ctx.Users.Where(u => u.UserId == userId);

            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                await users.ExecuteUpdateAsync(b =>
                    b.SetProperty(u => u.Username, _ => dto.Username.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                await users.ExecuteUpdateAsync(b =>
                    b.SetProperty(u => u.FullName, _ => dto.FullName.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                await users.ExecuteUpdateAsync(b =>
                    b.SetProperty(u => u.Email, _ => dto.Email.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                await users.ExecuteUpdateAsync(b =>
                    b.SetProperty(u => u.PhoneNumber, _ => dto.PhoneNumber.Trim()));
            }
        }



        public async Task<User?> GetWithPictureAsync(Guid userId) =>
            await _ctx.Users.FindAsync(userId);

        // ── Stats ─────────────────────────────────────────────
        public async Task<List<MonthlyStatDto>> GetMonthlyStatsAsync() =>
            await _ctx.Users
                .Where(u => u.EnrollmentDate.HasValue)
                .GroupBy(u => new { u.EnrollmentDate!.Value.Year, u.EnrollmentDate.Value.Month })
                .Select(g => new MonthlyStatDto {
                    Year  = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

        // ── Faculties ─────────────────────────────────────────
         public async Task<IEnumerable<Faculty>> GetAllFacultiesAsync()
        {
            return await _ctx.Faculties.ToListAsync();
        }

       public async Task<List<Student>> GetStudentsByFacultyAsync(int facultyId)
        {
            return await _ctx.Users
                .OfType<Student>()
                .Where(s => s.FacultyId == facultyId)
                .ToListAsync();
        }

         public async Task<Faculty?> GetFacultyByIdAsync(int facultyId)
        {
            return await _ctx.Faculties.FindAsync(facultyId);
        }

        public async Task<bool> FacultyCodeExistsAsync(string code) =>
            await _ctx.Faculties.AnyAsync(f => f.FacultyCode == code);

        public async Task<Faculty?> GetFacultyByNameAsync(string name)
        {
            // Normalize once
            var lowerName = name.Trim().ToLower();

            return await _ctx.Faculties
                .SingleOrDefaultAsync(f =>
                    f.FacultyName.ToLower() == lowerName);
        }



        public async Task AddFacultyAsync(Faculty f) =>
            await _ctx.Faculties.AddAsync(f);

        public void UpdateFaculty(Faculty f) =>
            _ctx.Faculties.Update(f);

        // ── Courses ───────────────────────────────────────────

        public async Task<IEnumerable<CourseListDto>> GetAllCoursesAsync()
        {
            return await _ctx.Courses
                .Select(c => new CourseListDto
                {
                    Id          = c.Id,
                    CourseCode  = c.CourseCode,
                    CourseName  = c.CourseName,
                    TeacherName = _ctx.Users
                                    .Where(u => u.UserId == c.UserId)
                                    .Select(u => u.FullName)
                                    .FirstOrDefault()?? "Unknown",
                    FacultyId   = c.FacultyId,
                    FacultyCode = _ctx.Faculties
                                    .Where(f => f.FacultyId == c.FacultyId)
                                    .Select(f => f.FacultyCode)
                                    .FirstOrDefault()
                })
                .ToListAsync();
        }


        public async Task<bool> CourseCodeExistsAsync(string courseCode) =>
            await _ctx.Courses.AnyAsync(c => c.CourseCode == courseCode);

        public async Task AddCourseAsync(Course c) =>
            await _ctx.Courses.AddAsync(c);

        public async Task<Course?> GetCourseByIdAsync(Guid id) =>
            await _ctx.Courses.FindAsync(id);

        public void UpdateCourse(Course c) =>
            _ctx.Courses.Update(c);

        public void RemoveCourse(Course c) =>
            _ctx.Courses.Remove(c);

        // ── Enrollments ───────────────────────────────────────
        public async Task<List<Enrollment>> ListAllEnrollmentsAsync() =>
            await _ctx.Enrollments
                      .Include(e => e.Course)
                      .ToListAsync();

        // ── Persistence ───────────────────────────────────────
        public Task<int> SaveChangesAsync()
        {
            return _ctx.SaveChangesAsync();
        }

        public async Task<bool> IsUserInRoleAsync(Guid userId, string roleName)
        {
            var role = await _ctx.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role == null) return false;
            return await _ctx.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.RoleId);
        }


    }
}
