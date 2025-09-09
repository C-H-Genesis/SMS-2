using Models;
using System.Text.Json.Serialization;


namespace Models
{
    public class Grades
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public Guid CourseId { get; set; }
        public Course? Course { get; set; }
        public double Score { get; set; }
        public required string Feedback { get; set; }
        public DateTime GradedAt { get; set; }
        public string? GradeText { get; set; }
        public required Guid SubmissionId { get; set; }
         public required int    EnrollmentId { get; set; } 
        
        public Submission? Submission { get; set; }
        public Enrollment Enrollment { get; set; } = default!;
    }

}