
namespace DTOs
{
    // DTOs/EnrollmentWithCourseDto.cs
public class EnrollmentWithCourseDto 
{
    public int    Id { get; set; }
    public Guid    CourseId { get; set; }
    public required string CourseName { get; set; } 
    public Guid UserId { get; set; } 
    public bool   Status { get; set; }
    public DateTime EnrollmentDate { get; set; }
}

}