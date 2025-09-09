using System;
using System.ComponentModel.DataAnnotations;   // ← needed for [Required] et al
using Microsoft.AspNetCore.Http;

namespace DTOs
{
    public class SubmissionDto
    {
        public Guid AssignmentId { get; set; }
        public Guid UserId { get; set; }
        public Guid CourseId { get; set; }
        public Guid? FileId { get; set; }
        public string? WrittenSubmission { get; set; }
        public DateTime SubmittedAt { get; set; }
        [DataType(DataType.Upload)]
        public IFormFile? File { get; set; }
        public string? FileUrl { get; set; } 
    }

}