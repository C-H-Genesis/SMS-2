
namespace DTOs
{
    // DTO definition
    public class UserDto
    {
        public Guid   UserId      { get; set; }
        public string FullName    { get; set; } = string.Empty;
        public string Username    { get; set; } = string.Empty;
        public string Email       { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string UserType    { get; set; } = string.Empty;
        public string? RegNumber  { get; set; } // Only for students
        public string? PasswordHash { get; set; }
        public bool IsActive { get; set; }
    }

}