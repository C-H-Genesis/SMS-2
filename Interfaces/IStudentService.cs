using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DTOs;
using Models;
using Microsoft.AspNetCore.Http;

namespace Interfaces
{
    public interface IStudentService
    {
        Task<IEnumerable<Course>> GetAllCoursesByStudentAsync(Guid userId);
        Task<ProfileDto?> GetProfileAsync(Guid userId);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<string?> RegisterCourseAsync(Guid userId, RegisterCourseDto dto);
        Task<IEnumerable<object>> GetRegisteredCoursesAsync(Guid userId);
        Task<IEnumerable<object>> GetAssignmentsForStudentAsync(Guid userId);
        Task<bool> UploadProfilePictureAsync(Guid userId, IFormFile file);
        Task<byte[]?> GetProfilePictureAsync(Guid userId);
        Task<object?> PostSubmissionAsync(Guid userId, SubmissionDto dto, string webRootPath, HttpRequest request);
        Task<UploadedFile?> GetUploadedFileByIdAsync(Guid fileId);
        Task<List<EnrollmentResultDto>> GetMyResultsAsync(Guid userId);
       
    }
}
