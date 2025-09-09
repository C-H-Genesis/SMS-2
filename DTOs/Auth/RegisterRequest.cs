
namespace DTOs
{
    public class RegisterRequest
    {
        public required string FullName { get; set; }
        public required string Username { get; set; }
        public required string Role { get; set; } // Student, Admin, Finance
        public required string Email { get; set; }
        public string? FacultyName { get; set; }
        public string? RegNumber { get; set; }
}

}