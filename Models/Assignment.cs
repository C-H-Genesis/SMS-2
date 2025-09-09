using Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace Models
{
    public class Assignments
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Title { get; set; } = string.Empty;
        public required string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public Guid? FileId { get; set; }
        [ForeignKey(nameof(FileId))]
        public UploadedFile? UploadedFile { get; set; }

        public Guid CourseId { get; set; }
        public Course? Course { get; set; }
        [ForeignKey("TeacherId")]
        public Guid TeacherId { get; set; }
        public User? Teacher { get; set; }
        public string? WrittenAssignment { get; set; }
        public ICollection<Submission>? Submissions { get; set; }
    }

}