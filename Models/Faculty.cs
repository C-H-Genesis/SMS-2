using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models;

namespace Models
{
    [Table("Faculties")]
    public class Faculty
    {
        /// <summary>
        /// Primary key (INT IDENTITY).
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FacultyId { get; set; }

        /// <summary>
        /// Full name of the faculty (e.g. "Information Technology").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FacultyName { get; set; } = string.Empty;

        /// <summary>
        /// 3-letter code used in RegNumber (e.g. "BIT", "ACC").
        /// </summary>
        [Required]
        [MaxLength(5)]
        public string FacultyCode { get; set; } = string.Empty;

        /// <summary>
        /// 
        /// Navigation to the courses under this faculty.
        /// </summary>
        public ICollection<Course>? Courses { get; set; }

        /// <summary>
        /// Navigation to the students assigned to this faculty.
        /// </summary>
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
