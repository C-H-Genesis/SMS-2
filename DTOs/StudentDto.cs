namespace DTOs
{
    public class StudentDto
    {
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? RegNumber { get; set; }
        public int FacultyId { get; set; }
        public string? UserType {get; set;}
        public bool IsActive { get; set;}
    }
} 