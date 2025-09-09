

namespace DTOs
{
    public class CourseListDto
{
    public Guid Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int? FacultyId { get; set; } 
    public string? FacultyCode { get; set; }
}

    
}