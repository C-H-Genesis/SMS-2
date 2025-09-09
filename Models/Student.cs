using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models;

namespace Models
{
    public class Student : User
    {
        /// <summary>
        /// e.g. "BIT/2025/D9B1F9C2"
        /// </summary>
        [Column("RegNumber")]
        public string RegNumber { get; set; } = string.Empty;

        /// <summary>
        /// The 3-letter code of the faculty (e.g. "BIT", "ACC")
        /// </summary>
        public int FacultyId { get; set; }
        public Faculty? Faculty { get; set; }

        /// <summary>
        /// Inherited from User: EnrollmentDate → use its Year for your RegNumber’s YYYY
        /// </summary>
        // public DateTime? EnrollmentDate { get; set; }  // already on User
        

        
    }
}
