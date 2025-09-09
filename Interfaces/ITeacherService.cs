using DTOs;
using Models;

namespace Interfaces
{
        public interface ITeacherService
    {
        Task<ProfileDto?> GetProfileAsync(Guid teacherId);
        Task<bool> UpdateProfileAsync(Guid teacherId, UpdateProfileDto dto);
        Task<bool> ChangePasswordAsync(Guid teacherId, ChangePasswordRequest request);
        Task<IEnumerable<Course>> GetCoursesAsync(Guid teacherId);
        Task<IEnumerable<StudentDto>> GetStudentsByCourseAsync(string courseCode);
        Task<UploadedFileDto?> UploadAssignmentFileAsync(IFormFile file, HttpRequest request, string webRootPath);
        Task<AssignmentDto> CreateAssignmentAsync(AssignmentDto dto, Guid teacherId);
        Task<IEnumerable<AssignmentDto>> GetAllAssignmentsAsync(Guid teacherId);
        Task<IEnumerable<SubmissionForTeacherDto>> GetAllSubmissionsAsync(Guid teacherId);
        Task<IEnumerable<SubmissionForTeacherDto>> GetSubmissionsByCourseAsync(Guid teacherId, Guid courseId);
        Task<Submission?> GetSubmissionByIdAsync(Guid id);
        Task<bool> UpdateSubmissionAsync(Guid id, SubmissionDto dto);
        Task<bool> DeleteSubmissionAsync(Guid id);
        Task<string> GradeSubmissionAsync(Guid id, GradeDto dto, Guid teacherId);
        Task<string> UpdateSubmissionGradeAsync(Guid id, GradeDto dto, Guid teacherId);
        Task<UploadedFile?> GetUploadedFileByIdAsync(Guid fileId);
    }

}