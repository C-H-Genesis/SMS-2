using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs
{
    public class UpdateCourseDto
{
    [Required]
    public string CourseCode { get; set; } = string.Empty;

    [Required]
    public string CourseName { get; set; } = string.Empty;

    [Required]
    public string FacultyName { get; set; } = string.Empty;

    [Required]
    public string TeacherName { get; set; } = string.Empty;
}
}