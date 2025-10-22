using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;    
using System.Security.Claims;
using System.Text;
using System.Collections.Generic; // For List<T> 
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Models;
using ApplicationDbContext;
using System.Security.Cryptography;
using DTOs;
using EmailAuth;
using Microsoft.AspNetCore.Cors; 


namespace AuthController
{
    [ApiController]
    [Route("api/auth")]
    [EnableCors("AllowAll")]
    public class AuthController : ControllerBase
    {
        private readonly SMSDbContext _context;
        private readonly EmailService _emailService;

        public AuthController(SMSDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

         [HttpGet("GetAllFaculties")]
        public async Task<IActionResult> GetFaculties()
        {
            var faculty = await _context.Faculties.ToListAsync();
            return Ok(faculty);
        }

        [HttpGet("avatar")]
        [Authorize]   // only calls with a valid JWT may reach this code
        public async Task<IActionResult> GetMyAvatar()
        {
            // 1) Read the UserId claim from the validated token:
            var idClaim = User.FindFirst("UserId")?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
                return Unauthorized();  // malformed or missing UserId

            // 2) Look up the user & their picture bytes
            var user = await _context.Users.FindAsync(userId);
            if (user?.ProfilePicture is null || user.ProfilePicture.Length == 0)
                return NotFound();

            // 3) Send back the binary + mime type
            return File(user.ProfilePicture, user.ProfilePictureType!);
        }

        // Registration Method
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // 1) Username check
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest("Username already exists.");

            // 2) Role lookup
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == request.Role);
            if (role == null)
                return BadRequest("Invalid role specified.");

            // 3) Password hashing
            string generatedPassword = PasswordGenerator.GeneratePassword(12);
            string hashedPassword    = BCrypt.Net.BCrypt.HashPassword(generatedPassword);

            string? regNumber = null;

            User user;

            // 4) Branch by role
            if (request.Role == "Student")
            {
                // Faculty lookup
                if (string.IsNullOrWhiteSpace(request.FacultyName))
                    return BadRequest("Faculty name is required for student registration.");

                var faculty = await _context.Faculties
                    .FirstOrDefaultAsync(f => f.FacultyName.ToLower() == request.FacultyName.ToLower());

                if (faculty == null)
                    return BadRequest($"Faculty '{request.FacultyName}' does not exist.");

                var enrollmentDate = DateTime.UtcNow;
                var year = enrollmentDate.Year;
                var fac = faculty.FacultyCode.Substring(0, 3).ToUpper(); // Ensure FacultyCode exists
                var shortGuid = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper(); // 4-5 chars

                regNumber = $"{fac}/{year}/{shortGuid}";    

                // Create Student
                user = new Student
                {
                    UserId = Guid.NewGuid(),
                    FullName = request.FullName,
                    Username = request.Username,
                    PasswordHash = hashedPassword,
                    RoleId = role.RoleId,
                    UserType = "Student",
                    EnrollmentDate = DateTime.UtcNow,
                    Email = request.Email,
                    FacultyId = faculty.FacultyId,
                    RegNumber = regNumber
                };
            }
            else
            {
                // Non-student roles
                user = request.Role switch
                {
                    "Admin" => new Admin
                    {
                        UserId       = Guid.NewGuid(),
                        FullName     = request.FullName,
                        Username     = request.Username,
                        PasswordHash = hashedPassword,
                        RoleId       = role.RoleId,
                        Email        = request.Email,
                        EnrollmentDate = DateTime.UtcNow,
                        UserType     = "Admin"
                    },
                    "Finance" => new Finance
                    {
                        UserId       = Guid.NewGuid(),
                        FullName     = request.FullName,
                        Username     = request.Username,
                        PasswordHash = hashedPassword,
                        RoleId       = role.RoleId,
                        Email        = request.Email,
                        EnrollmentDate = DateTime.UtcNow,
                        UserType     = "Finance"
                    },
                    "Teacher" => new Teacher
                    {
                        UserId       = Guid.NewGuid(),
                        FullName     = request.FullName,
                        Username     = request.Username,
                        PasswordHash = hashedPassword,
                        RoleId       = role.RoleId,
                        Email        = request.Email,
                        EnrollmentDate = DateTime.UtcNow,
                        UserType     = "Teacher"
                    },
                    _ => throw new ArgumentException("Invalid role specified.")
                };
            }

            // 5) Persist user and role link
            _context.Users.Add(user);
            await _context.UserRoles.AddAsync(new UserRole
            {
                UserId = user.UserId,
                RoleId = role.RoleId
            });

            int save = await _context.SaveChangesAsync();
            if (save <= 0)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ProblemDetails { Title = "Registration failed.", Detail = "Could not save user." });

            // 6) Send welcome email
            try
            {
                var subject = "Welcome to SMS-LMS Platform";
                var body = new StringBuilder();
                body.AppendLine($"Hi {request.FullName},");
                body.AppendLine();
                body.AppendLine($"You have successfully registered as a {request.Role}.");
                body.AppendLine($"Your username is: {request.Username}");
                body.AppendLine($"Your temporary password is: {generatedPassword}");

                // **only for students**:
                if (request.Role == "Student" && regNumber != null)
                {
                    body.AppendLine($"Your registration number is: {regNumber}");
                }

                body.AppendLine();
                body.AppendLine("Please change your password after logging in.");
                body.AppendLine();
                body.AppendLine("Thank you!");

                await _emailService.SendEmailAsync(request.Email, subject, body.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send failed: {ex.Message}");
            }

                return Ok(new { message = "Registration successful and email sent." });
        }



        // Login Method
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var user = await _context.Users
            .Where(u => u.Username == request.Username)
             .Select(u => new LoginUserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                PasswordHash = u.PasswordHash,
                IsActive = u.IsActive,
                Roles = u.Roles.Select(r => new RoleDto
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName
                }).ToList()
            })
            .FirstOrDefaultAsync();


            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            if (!user.IsActive)
              return Unauthorized(new { message = "This account has been disabled." });


            // Verify the input password matches the hashed password in the database
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return Unauthorized("Invalid username or password.");
            }

           
                var token = GenerateToken(user);
                return Ok(new { token = token });

        }

        //            Reset Password             //

                [HttpPost("reset-password")]
                public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
                {
                    var trimmedToken = request.Token?.Trim();

                    var user = await _context.Users.FirstOrDefaultAsync(u =>
                        u.Email == request.Email &&
                        u.PasswordResetToken == trimmedToken &&
                        u.ResetTokenExpiry > DateTime.UtcNow);
   

                    if (user == null)
                        return BadRequest("Invalid or expired reset token.");

                    // Hash and set the new password
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                    // Clear token fields
                    user.PasswordResetToken = null;
                    user.ResetTokenExpiry = null;

                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Your password has been reset successfully."});
                }


        //               Forgot Password                 //
                    [HttpPost("forgot-password")]
            public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest("Email is required.");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                    return NotFound("No user associated with this email.");

                // Generate reset token
                var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
                user.PasswordResetToken = resetToken;
                user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Token valid for 1 hour

                await _context.SaveChangesAsync();

                // Send reset link
                var encodedToken = Uri.EscapeDataString(resetToken);
                var resetLink = $"http://localhost:4200/reset-password?token={encodedToken}&email={request.Email}";
                var subject = "Password Reset Request";
                var body = $"Hi {user.FullName},\n\nClick the link below to reset your password:\n{resetLink}\n\nIf you didn’t request this, ignore this email.";

                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendEmailAsync(user.Email, subject, body);
                }


                return Ok(new { message = "Password reset link has been sent to your email."});
            }


        // Token generation method (unchanged)
            private string GenerateToken(LoginUserDto user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "User object cannot be null.");
            }

            if (string.IsNullOrEmpty(user.Username))
            {
                throw new InvalidOperationException("User.Username cannot be null or empty.");
            }

            if (user.Roles == null || !user.Roles.Any())
            {
                throw new InvalidOperationException("User role information is not available.");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("UserId", user.UserId.ToString())
            };

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisisYourSecretKeyHereof32bytes"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "YourIssuer",
                audience: "YourAudience",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }


            public class PasswordGenerator
        {
            public static string GeneratePassword(int length = 12)
            {
                const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()-_=+";
                
                if (length <= 0)
                    throw new ArgumentException("Password length must be greater than 0.");

                var chars = new char[length];
                using (var rng = RandomNumberGenerator.Create())
                {
                    byte[] uintBuffer = new byte[sizeof(uint)];

                    for (int i = 0; i < length; i++)
                    {
                        rng.GetBytes(uintBuffer);
                        uint num = BitConverter.ToUInt32(uintBuffer, 0);
                        chars[i] = valid[(int)(num % (uint)valid.Length)];
                    }
                }

                return new string(chars);
            }
        }
}


