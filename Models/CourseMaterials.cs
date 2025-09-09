using Models;

namespace Models
{
        public class Materials
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Title { get; set; }
        public required string FileUrl { get; set; }
        public DateTime UploadedAt { get; set; }
        public Guid CourseId { get; set; }
        public Course? Course { get; set; }
    }

}