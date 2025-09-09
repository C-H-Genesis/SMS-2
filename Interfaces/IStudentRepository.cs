using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;
using DTOs;



namespace Interfaces
{
    public interface IStudentRepository
    {
        Task<Student?> GetStudentByIdAsync(Guid userId);
        Task<Student?> GetUserByIdAsync(Guid userId);
        Task<IEnumerable<Course>> GetCoursesByFacultyIdAsync(int facultyId);
        Task<IEnumerable<Enrollment>> GetEnrollmentsByUserIdAsync(Guid userId);
        Task<IEnumerable<Assignments>> GetAssignmentsForStudentAsync(Guid userId);
        Task SaveUploadedFileAsync(UploadedFile file);
        Task SaveSubmissionAsync(Submission submission);
        Task<UploadedFile?> GetUploadedFileByIdAsync(Guid id);
        Task SaveChangesAsync();
        Task<List<EnrollmentResultDto>> GetStudentResultsAsync(Guid userId);

        // 2) Fetch coursework scores for an enrollment
        Task<List<Grades>> GetGradesByEnrollmentIdAsync(int enrollmentId);

        // 3) Fetch the raw Enrollment entity
        Task<Enrollment?> GetEnrollmentEntityAsync(Guid userId, Guid CourseId);
        Task<(Guid Id, string CourseName)?> GetCourseInfoByCodeAsync(string courseCode);
        Task<Enrollment?> GetEnrollmentAsync(Guid userId, Guid courseId);
        Task AddEnrollmentAsync(Enrollment e);



    }
}
