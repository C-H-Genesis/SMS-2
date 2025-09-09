using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs
{
        public class AssignmentDto
    {
        public Guid Id { get; set; }
         [Required]
        public required  string Title { get; set; }

        [Required]
        public required string Description { get; set; }
        public string? WrittenAssignment { get; set;}

        [Required]
        public DateTime DueDate { get; set; }

        [Required]  // Ensure CourseId is mandatory
        public Guid CourseId { get; set; }

        public string? CourseName { get; set; }

        public Guid? FileId  { get; set; }

        [Required]
        public Guid TeacherId { get; set; }
        public DateTime? CreatedAt { get; set; }
         
    }
}