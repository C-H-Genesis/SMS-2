

namespace DTOs
{
        public class GradeDto
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public double Score { get; set; }
        public required string Feedback { get; set; }
        
    }
}