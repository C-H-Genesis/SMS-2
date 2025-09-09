using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Models;
using Repositories;
using DTOs;
using EmailAuth;
using AuthController;
using ApplicationDbContext;


namespace Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _repo;
        private readonly EmailService      _email;
        private readonly SMSDbContext     _ctx;
        

        public AdminService(IAdminRepository repo, EmailService email, SMSDbContext ctx)
        {
            _repo  = repo;
            _email = email;
            _ctx = ctx;
        }

        // ── Profile Picture ───────────────────────────────────
        public async Task UploadProfilePictureAsync(Guid userId, IFormFile file)
        {
            var u = await _repo.GetEntityByIdAsync(userId)
                  ?? throw new KeyNotFoundException("User not found");
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            u.ProfilePicture     = ms.ToArray();
            u.ProfilePictureName = file.FileName;
            u.ProfilePictureType = file.ContentType;
            _repo.UpdateUser(u);
            await _repo.SaveChangesAsync();
        }

        public async Task<(byte[] Data, string Name, string Type)?> GetProfilePictureAsync(Guid userId)
        {
            var u = await _repo.GetWithPictureAsync(userId);
            if (u?.ProfilePicture == null) return null;
            return (u.ProfilePicture, u.ProfilePictureName!, u.ProfilePictureType!);
        }

        // ── Profile Data ──────────────────────────────────────
           public Task<UpdateAdminProfileDto?> GetProfileAsync(Guid userId)
            {
                // delegate to the new, lightweight repo method:
                return _repo.GetProfileDtoByIdAsync(userId);
            }


        public Task UpdateProfileAsync(Guid userId, UpdateAdminProfileDto dto) =>
            _repo.UpdateUserProfileAsync(userId, dto);



        // ── User CRUD & Stats ─────────────────────────────────

        public async Task<bool> UserExistsAsync(Guid userId) =>
            await _repo.AnyUserWithIdAsync(userId);
            
       public async Task<List<UserDto>> GetAllUsersAsync() =>
        await _repo.GetAllUserDtosAsync();


        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var u = await _repo.GetEntityByIdAsync(id);
            if (u == null) return null;

            string? regNumber = null;

            if (u is Student student)
            {
                regNumber = student.RegNumber;
            }

            return new UserDto
            {
                UserId      = u.UserId,
                FullName    = u.FullName,
                Username    = u.Username,
                Email       = u.Email!,
                PhoneNumber = u.PhoneNumber!,
                UserType    = u.UserType!,
                RegNumber   = regNumber,
                IsActive    = u.IsActive
            };
        }


        public async Task<List<UserDto>> GetUsersByRoleAsync(string role) =>
            (await _repo.GetByRoleAsync(role))
            .Select(u => new UserDto {
                UserId      = u.UserId,
                FullName    = u.FullName,
                Username    = u.Username,
                Email       = u.Email!,
                PhoneNumber = u.PhoneNumber!,
                UserType    = u.UserType!,
                IsActive    = u.IsActive
            }).ToList();

        public async Task<List<MonthlyStatDto>> GetMonthlyUserStatsAsync() =>
            (await _repo.GetMonthlyStatsAsync())
            .Select(t => new MonthlyStatDto { Year = t.Year, Month = t.Month, Count = t.Count })
            .ToList();

        // ── Manage Users ──────────────────────────────────────
        public async Task CreateUserAsync(RegisterRequest request)
        {
            // 2) Role
            var role = await _repo.GetRoleByNameAsync(request.Role)
                       ?? throw new InvalidOperationException($"Role '{request.Role}' not found");

            // 3) Password
            var pwd  = PasswordGenerator.GeneratePassword(12);
            var hash = BCrypt.Net.BCrypt.HashPassword(pwd);

            // 4) Build user
            User user; string? reg = null;
            if (request.Role == "Student")
            {
                if (string.IsNullOrWhiteSpace(request.FacultyName))
                    throw new InvalidOperationException("Faculty required");
                var fac = await _repo.GetFacultyByNameAsync(request.FacultyName)
                          ?? throw new InvalidOperationException("Faculty not found");
                var year    = DateTime.UtcNow.Year;
                var shortId = Guid.NewGuid().ToString("N").Substring(0,5).ToUpperInvariant();
                reg = $"{fac.FacultyCode.Substring(0,3)}/{year}/{shortId}";
                user = new Student {
                    UserId         = Guid.NewGuid(),
                    FullName       = request.FullName,
                    Username       = request.Username,
                    PasswordHash   = hash,
                    Email          = request.Email,
                    UserType       = "Student",
                    RoleId         = role.RoleId,
                    EnrollmentDate = DateTime.UtcNow,
                    FacultyId      = fac.FacultyId,
                    RegNumber      = reg
                };
            }
            else if (request.Role == "Admin")
            {
                user = new Admin {
                    UserId         = Guid.NewGuid(),
                    FullName       = request.FullName,
                    Username       = request.Username,
                    PasswordHash   = hash,
                    Email          = request.Email,
                    UserType       = "Admin",
                    RoleId         = role.RoleId,
                    EnrollmentDate = DateTime.UtcNow
                };
            }
            else if (request.Role == "Teacher")
            {
                user = new Teacher {
                    UserId         = Guid.NewGuid(),
                    FullName       = request.FullName,
                    Username       = request.Username,
                    PasswordHash   = hash,
                    Email          = request.Email,
                    UserType       = "Teacher",
                    RoleId         = role.RoleId,
                    EnrollmentDate = DateTime.UtcNow
                };
            }
            else if (request.Role == "Finance")
            {
                user = new Finance {
                    UserId         = Guid.NewGuid(),
                    FullName       = request.FullName,
                    Username       = request.Username,
                    PasswordHash   = hash,
                    Email          = request.Email,
                    UserType       = "Finance",
                    RoleId         = role.RoleId,
                    EnrollmentDate = DateTime.UtcNow
                };
            }
            else throw new InvalidOperationException("Invalid role");

            // 5) Persist
            await _repo.AddUserAsync(user);
            await _repo.AddUserRoleAsync(new UserRole {
                UserId = user.UserId,
                RoleId = role.RoleId
            });
            await _repo.SaveChangesAsync();

            // 6) Email
            var sb = new StringBuilder();
            sb.AppendLine($"Hi {request.FullName},")
              .AppendLine($"You are registered as {request.Role}")
              .AppendLine($"Username: {request.Username}")
              .AppendLine($"Password: {pwd}");
            if (reg != null) sb.AppendLine($"Reg#: {reg}");
            await _email.SendEmailAsync(request.Email, "Welcome", sb.ToString());
        }

        public async Task UpdateUserInfoAsync(Guid userId, UpdateUserInfo dto)
        {
            var u = await _repo.GetEntityByIdAsync(userId)
                  ?? throw new KeyNotFoundException("User not found");
            if (!string.IsNullOrWhiteSpace(dto.UserName))    u.Username    = dto.UserName;
            if (!string.IsNullOrWhiteSpace(dto.FullName))    u.FullName    = dto.FullName;
            if (!string.IsNullOrWhiteSpace(dto.Email))       u.Email       = dto.Email;
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) u.PhoneNumber = dto.PhoneNumber;
            _repo.UpdateUser(u);
            await _repo.SaveChangesAsync();
        }

        public async Task SetUserActiveStateAsync(Guid userId, bool isActive, Guid requesterId)
        {
            if (userId == Guid.Empty) throw new ArgumentException("userId required");
            // authorize requester if needed
            var requesterIsAdmin = await _repo.IsUserInRoleAsync(requesterId, "Admin");
            if (!requesterIsAdmin)
                throw new UnauthorizedAccessException("Only Admins may enable/disable users.");

            var user = await _repo.GetUserByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            // If disabling, protect last admin
            if (!isActive)
            {
                var hasAdminRole = await _repo.IsUserInRoleAsync(userId, "Admin");
                if (hasAdminRole)
                {
                    var adminsCount = await _repo.CountUsersInRoleAsync("Admin");
                    if (adminsCount <= 1)
                        throw new InvalidOperationException("Cannot disable the last Admin in the system.");
                }
            }

            // Mark for change
            await _repo.SetUserActiveStateAsync(userId, isActive);

            var changed = await _repo.SaveChangesAsync();
            if (changed == 0)
                throw new InvalidOperationException("Failed to update user state.");
        }


        public async Task<List<RoleDto>> GetAllRolesAsync(Guid requesterId)
        {
            var roles = await _repo.GetAllRolesAsync();
            return roles;
        }

       public async Task AssignRolesAsync(Guid userId, List<string> roles, Guid requesterId)
    {
        if (roles == null || !roles.Any())
            throw new ArgumentException("No roles provided.", nameof(roles));

        // authorize: only Admin (example). Adjust to your policy.
        var isRequesterAdmin = await _repo.IsUserInRoleAsync(requesterId, "Admin");
        if (!isRequesterAdmin)
            throw new UnauthorizedAccessException("Only Admins may assign roles.");

        // normalize & dedupe
        var normalized = roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!normalized.Any())
            throw new ArgumentException("No valid role names provided.", nameof(roles));

        // 1) verify target user exists
        var user = await _repo.GetUserByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        // 2) existing roles for user (to skip duplicates)
        var existingUserRoles = await _repo.GetUserRolesByUserIdAsync(userId); // returns List<UserRole>
        var existingRoleIds = existingUserRoles.Select(ur => ur.RoleId).ToHashSet();

        // 3) for each role name, resolve role and add if missing
        var addedAny = false;
        foreach (var rn in normalized)
        {
            var role = await _repo.GetRoleByNameAsync(rn);
            if (role == null)
            {
                throw new InvalidOperationException($"Invalid role: {rn}");
            }

            if (existingRoleIds.Contains(role.RoleId))
            {
                continue; // already assigned
            }

            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = role.RoleId
            };

            await _repo.AddUserRoleAsync(userRole);
            existingRoleIds.Add(role.RoleId);
            addedAny = true;
        }

        if (addedAny)
        {
            // persist changes via repo (assumes repo.SaveChangesAsync flushes the same DbContext)
            var changed = await _repo.SaveChangesAsync();
            if (changed == 0)
                throw new InvalidOperationException("Failed to persist role assignments.");
        }
        // if nothing added, fine — it's idempotent
    }
        
        // AdminService.cs
        public async Task RemoveRoleFromUserAsync(Guid userId, Guid roleId, Guid requesterId)
        {
            // Optional: authorize requester (example: require Admin)
            var isRequesterAdmin = await _repo.IsUserInRoleAsync(requesterId, "Admin");
            if (!isRequesterAdmin)
                throw new UnauthorizedAccessException("Only Admins may remove roles.");

            // Validate user
            var user = await _repo.GetUserByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            // protect last role for user
            var userRoleCount = await _repo.GetUserRoleCountAsync(userId);
            if (userRoleCount <= 1)
                throw new InvalidOperationException("Cannot remove the user's last role. Assign another role first.");

            // Resolve role by id
            var role = await _repo.GetRoleByIdAsync(roleId);
            if (role == null) throw new KeyNotFoundException($"Role with id '{roleId}' not found.");

            // protect last admin in system
            if (string.Equals(role.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                var adminsCount = await _repo.CountUsersInRoleAsync("Admin");
                if (adminsCount <= 1)
                    throw new InvalidOperationException("Cannot remove the last Admin from the system.");
            }

           await _repo.RemoveUserRoleByIdsAsync(userId, roleId);

            // Persist once
            var changed = await _repo.SaveChangesAsync();
            if (changed == 0)
                throw new InvalidOperationException("Failed to remove role (no rows affected).");
        }


        // optionally you can expose helper to fetch current role names (recommended)
        public Task<List<string>> GetUserRoleNamesAsync(Guid userId) =>
          _repo.GetUserRoleNamesAsync(userId);


        public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest req)
        {
            // 1) Fetch just the hash
            var existingHash = await _repo.GetPasswordHashByUserIdAsync(userId);
            if (existingHash is null)
                throw new KeyNotFoundException("User not found.");

            // 2) Validate current password
            if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, existingHash))
                throw new InvalidOperationException("Current password incorrect.");

            // 3) Hash new password
            var newHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

            // 4) Update only that column
            var rows = await _repo.UpdatePasswordHashByUserIdAsync(userId, newHash);
            if (rows == 0)
                throw new KeyNotFoundException("User not found."); // concurrency or missing
        }

        public async Task DeleteUserAsync(Guid userId)
        {
            var user = await _repo.GetEntityByIdAsync(userId)
               ?? throw new KeyNotFoundException("User not found.");

            // 1) Remove any UserRoles for this user
            _repo.DeleteUserRolesByUserId(userId);

            // 2) Now delete the user
            _repo.DeleteUser(user);

            // 3) Persist both deletions in one SaveChanges call
            await _repo.SaveChangesAsync();
        }

        // ── Faculties & Courses ───────────────────────────────
        public async Task<IEnumerable<UpdateFacultyDto>> GetAllFacultiesAsync()
        {
            var faculties = await _repo.GetAllFacultiesAsync();
            return faculties.Select(f => new UpdateFacultyDto
            {
                FacultyId = f.FacultyId,
                FacultyCode = f.FacultyCode,
                FacultyName = f.FacultyName
            });
        }

        public async Task<List<StudentDto>> GetStudentsByFacultyAsync(int facultyId)
        {
            var students = await _repo.GetStudentsByFacultyAsync(facultyId);

            return students.Select(s => new StudentDto
            {
                UserId = s.UserId,
                Username = s.Username, 
                FullName = s.FullName,
                Email = s.Email,
                RegNumber = s.RegNumber,
                UserType = s.UserType,
                FacultyId = s.FacultyId
            }).ToList();
        }

        public async Task<UpdateFacultyDto?> GetFacultyByIdAsync(int facultyId)
        {
            var faculty = await _repo.GetFacultyByIdAsync(facultyId);
            return faculty is null ? null : new UpdateFacultyDto
            {
                FacultyId = faculty.FacultyId,
                FacultyCode = faculty.FacultyCode,
                FacultyName = faculty.FacultyName
            };
        }

        public async Task CreateNewFacultyAsync(CreateNewFacultyDto dto)
        {
            if (await _repo.FacultyCodeExistsAsync(dto.FacultyCode))
                throw new InvalidOperationException("Code exists");
            await _repo.AddFacultyAsync(new Faculty {
                FacultyCode = dto.FacultyCode,
                FacultyName = dto.FacultyName
            });
            await _repo.SaveChangesAsync();
        }

        public async Task UpdateFacultyAsync(UpdateFacultyDto dto)
        {
            var f = await _repo.GetFacultyByIdAsync(dto.FacultyId)
                  ?? throw new KeyNotFoundException("Not found");
            f.FacultyName = dto.FacultyName.Trim();
            f.FacultyCode = dto.FacultyCode.Trim().ToUpperInvariant();
            _repo.UpdateFaculty(f);
            await _repo.SaveChangesAsync();
        }

        public async Task<IEnumerable<CourseListDto>> GetAllCoursesAsync()
        {
            var courses = await _repo.GetAllCoursesAsync();
            return courses.Select(c => new CourseListDto
            {
                Id = c.Id,
                CourseCode = c.CourseCode,
                CourseName = c.CourseName,
                TeacherName = c.TeacherName,  
                FacultyCode = c.FacultyCode
            });
        }

        public async Task CreateCourseAsync(AddCourseDto dto)
        {
            if (await _repo.CourseCodeExistsAsync(dto.CourseCode))
                throw new InvalidOperationException("Course code exists");

            // 1) find teacher
            var teacher = await _repo.GetByFullNameAsync(dto.TeacherName)
                        ?? throw new KeyNotFoundException($"Teacher '{dto.TeacherName}' not found");

            // 2) find faculty by name
            var faculty = await _repo.GetFacultyByNameAsync(dto.FacultyName)
                        ?? throw new KeyNotFoundException($"Faculty '{dto.FacultyName}' not found");

            // 3) build the course
            var course = new Course
            {
                Id          = Guid.NewGuid(),
                CourseCode  = dto.CourseCode.Trim(),
                CourseName  = dto.CourseName.Trim(),
                UserId      = teacher.UserId,
                FacultyId   = faculty.FacultyId,
                User        = teacher,
                Faculty     = faculty,
                Enrollments = new List<Enrollment>(),
                Assignments = new List<Assignments>()
            };

            await _repo.AddCourseAsync(course);
            await _repo.SaveChangesAsync();
        }


        public async Task UpdateCourseAsync(Guid id, UpdateCourseDto dto)
        {
             var c = await _repo.GetCourseByIdAsync(id)
                ?? throw new KeyNotFoundException("Course not found");

            // trim & assign
            c.CourseCode = dto.CourseCode.Trim();
            c.CourseName = dto.CourseName.Trim();

            // teacher by username as before
            var teacher = await _repo.GetByFullNameAsync(dto.TeacherName)
                        ?? throw new KeyNotFoundException("Teacher not found");
            c.UserId = teacher.UserId;

            // now faculty by name
            var faculty = await _repo.GetFacultyByNameAsync(dto.FacultyName)
                        ?? throw new KeyNotFoundException($"Faculty '{dto.FacultyName}' not found");
            c.FacultyId = faculty.FacultyId;
            c.Faculty   = faculty;   // if you’re populating navigation

            _repo.UpdateCourse(c);
            await _repo.SaveChangesAsync();
        }

        public async Task DeleteCourseAsync(Guid id)
        {
            var c = await _repo.GetCourseByIdAsync(id)
                  ?? throw new KeyNotFoundException("Course not found");
            _repo.RemoveCourse(c);
            await _repo.SaveChangesAsync();
        }

        // ── Enrollments ───────────────────────────────────────
        public async Task<List<EnrollmentWithCourseDto>> GetAllEnrollmentsAsync()
        {
            var list = await _repo.ListAllEnrollmentsAsync();
            return list.Select(e => new EnrollmentWithCourseDto {
                Id             = e.Id,
                CourseId       = e.CourseId,
                CourseName     = e.Course?.CourseName ?? "Unknown",
                UserId         = e.UserId,
                Status         = e.Status,
                EnrollmentDate = e.EnrollmentDate
            }).ToList();
        }
    }
}
