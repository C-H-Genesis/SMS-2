
namespace DTOs
{
    public class EnrollmentResultDto
    {
        public Guid   CourseId      { get; set; }
        public string? CourseCode    { get; set; }
        public string CourseName    { get; set; } = default!;
        public double? AverageScore { get; set; }
        public string? LetterGrade  { get; set; }
        public DateTime EnrollmentDate { get; set; }
    }

}