using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models;

namespace Models
{
    public class Enrollment
    {
        public int Id { get; set; }
        public required Guid CourseId { get; set; }
        
        public Guid UserId { get; set; }
        public required bool Status{ get; set; }
        public DateTime EnrollmentDate { get; set; }
        public double? AverageScore  { get; set; }
        public string? LetterGrade   { get; set; }

         
        public Course? Course { get; set; }
        public User? User { get; set; }
        public ICollection<Grades>? Grade { get; set; } = new List<Grades>();

    } 
}


