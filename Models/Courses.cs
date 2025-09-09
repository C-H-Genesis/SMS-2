using Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Models
{
    public class Course
    {
        public required Guid Id { get; set; }
        public required string CourseCode { get; set; }
        public required string CourseName { get; set; }
        public Guid UserId { get; set; }

        public User? User { get; set; }
        public required int FacultyId { get; set; }
        public required Faculty Faculty { get; set; }
        [JsonIgnore]
        public ICollection<Assignments> Assignments { get; set; } = new List<Assignments>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    }

}