// DTOs/SubmissionForTeacherDto.cs
using System;

namespace DTOs
{
    public class SubmissionForTeacherDto
    {
        public Guid SubmissionId { get; set; }
        public Guid AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string RegNumber { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }

        // One of these will be populated:
        public string? WrittenSubmission { get; set; }
        public Guid? FileId { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public double?   Score    { get; set; }
        public string?   Feedback { get; set; }
    }
}
