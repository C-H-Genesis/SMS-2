using Models;
using ApplicationDbContext;

namespace Interfaces
{
        public interface ITeacherRepository
    {
        Task<Teacher?> GetUserByIdAsync(Guid userId);
        Task<Course?> GetCourseByCodeAsync(string courseCode);
        Task<IEnumerable<Course>> GetCoursesByTeacherAsync(Guid teacherId);
        Task<IEnumerable<Enrollment>> GetEnrollmentsByCourseCodeAsync(string courseCode);
        Task<bool> CourseBelongsToTeacherAsync(Guid courseId, Guid teacherId);
        Task<IEnumerable<Assignments>> GetAssignmentsByTeacherIdAsync(Guid teacherId);
        Task<IEnumerable<Submission>> GetAllSubmissionsByTeacherAsync(Guid teacherId);
        Task<IEnumerable<Submission>> GetSubmissionsByCourseIdAsync(Guid courseId);
        Task<Submission?> GetSubmissionByIdAsync(Guid id);
        Task<UploadedFile?> GetUploadedFileAsync(Guid fileId);
        Task<Enrollment?> GetEnrollmentAsync(Guid userId, Guid courseId);
        Task SaveAsync();
        SMSDbContext Context { get; }
    }

}