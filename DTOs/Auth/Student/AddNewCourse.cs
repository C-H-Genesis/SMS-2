using Models;

namespace DTOs
{
    public class AddCourseDto
    {
        public required string CourseCode { get; set; } // Required
        public required string CourseName { get; set; } // Required
        public required string TeacherName { get; set; }
        public required string FacultyName {get; set;}
}
    
}

