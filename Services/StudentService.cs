using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DTOs;
using Interfaces;
using Microsoft.AspNetCore.Http;
using Models;

namespace Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _repository;

        public StudentService(IStudentRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Course>> GetAllCoursesByStudentAsync(Guid userId)
        {
            var student = await _repository.GetStudentByIdAsync(userId);

            if (student == null)
             return Enumerable.Empty<Course>();

            return await _repository.GetCoursesByFacultyIdAsync(student.FacultyId);
        }

        public async Task<ProfileDto?> GetProfileAsync(Guid userId)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            if (user is not Student student) return null;

            return new ProfileDto
            {
                FullName = user.FullName,
                Username = user.Username,
                RegNumber = student.RegNumber,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            if (user is not Student student) return false;

            user.Username = string.IsNullOrWhiteSpace(dto.UserName) ? user.Username : dto.UserName;
            user.FullName = string.IsNullOrWhiteSpace(dto.FullName) ? user.FullName : dto.FullName;
            user.Email = string.IsNullOrWhiteSpace(dto.Email) ? user.Email : dto.Email;
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? user.PhoneNumber : dto.PhoneNumber;
            student.RegNumber = string.IsNullOrWhiteSpace(dto.RegNumber) ? student.RegNumber : dto.RegNumber;

            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<string?> RegisterCourseAsync(Guid userId, RegisterCourseDto dto)
        {
            // 1) Minimal course lookup
            var courseInfo = await _repository.GetCourseInfoByCodeAsync(dto.CourseCode);
            if (courseInfo == null)
                return "Course not found.";

            var (courseId, courseName) = courseInfo.Value;

            // 2) Load exactly this student+course enrollment (or null)
            var existing = await _repository.GetEnrollmentAsync(userId, courseId);
            if (existing == null)
            {
                // ➡️ First‐time: insert a single new row
                var e = new Enrollment {
                    UserId         = userId,
                    CourseId       = courseId,
                    Status         = true,
                    EnrollmentDate = DateTime.UtcNow
                };
                await _repository.AddEnrollmentAsync(e);
                await _repository.SaveChangesAsync();
                return $"Successfully registered for {courseName}.";
            }

            // 3) Already enrolled – pull only THIS enrollment’s grades
            var grades = await _repository.GetGradesByEnrollmentIdAsync(existing.Id);

            if (grades.Count == 0)
            {
                // No grades yet → allow “re‐register”
                existing.Status = false;
                await _repository.SaveChangesAsync();
                return $"Re‐registered for {courseName}. Awaiting grades.";
            }

            // 4) Compute average of THESE grades
            var avg = grades.Average(g => g.Score);
            var letter = ConvertScoreToLetter((int)Math.Round(avg));
            bool passed = avg >= 50 && grades.All(g => g.GradeText != "F");

            if (!passed)
            {
                existing.Status = false;
                await _repository.SaveChangesAsync();
                return $"Re‐registered for {courseName}. Previous average {avg:F1} ({letter}).";
            }

            // 5) Already passed – no DB change
            return $"You have already passed {courseName} with average {avg:F1} ({letter}).";
        }

        private string ConvertScoreToLetter(double score)
        {
            if (score >= 97) return "A+";
            if (score >= 93) return "A";
            if (score >= 90) return "A-";
            if (score >= 87) return "B+";
            if (score >= 83) return "B";
            if (score >= 80) return "B-";
            if (score >= 77) return "C+";
            if (score >= 73) return "C";
            if (score >= 70) return "C-";
            if (score >= 67) return "D+";
            if (score >= 63) return "D";
            if (score >= 60) return "D-";
            return "F";
        }




        public async Task<IEnumerable<object>> GetRegisteredCoursesAsync(Guid userId)
        {
            var enrollments = await _repository.GetEnrollmentsByUserIdAsync(userId);

            return enrollments.Select(e => new
            {
                CourseCode = e.Course?.CourseCode ?? "N/A",
                CourseName = e.Course?.CourseName ?? "N/A",
                e.Status,
                e.EnrollmentDate
            });
        }

        public async Task<IEnumerable<object>> GetAssignmentsForStudentAsync(Guid userId)
        {
            var assignments = await _repository.GetAssignmentsForStudentAsync(userId);

            return assignments.Select(a =>
            {
                var mySub = a.Submissions?.FirstOrDefault();
                return new
                {
                    a.Id,
                    a.Title,
                    a.Description,
                    a.DueDate,
                    a.CreatedAt,
                    a.WrittenAssignment,
                    a.CourseId,
                    CourseName = a.Course?.CourseName ?? "N/A",
                    a.TeacherId,
                    Grade = mySub?.Grade?.Score,
                    Feedback = mySub?.Grade?.Feedback,
                    GradeText = mySub?.Grade?.GradeText,
                    File = a.UploadedFile == null ? null : new
                    {
                        a.UploadedFile.FileId,
                        a.UploadedFile.FileName,
                        a.UploadedFile.ContentType,
                        a.UploadedFile.Content
                    }
                };
            });
        }

        public async Task<bool> UploadProfilePictureAsync(Guid userId, IFormFile file)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            if (user == null) return false;

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            user.ProfilePicture = bytes;
            user.ProfilePictureName = file.FileName;
            user.ProfilePictureType = file.ContentType;

            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]?> GetProfilePictureAsync(Guid userId)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            return user?.ProfilePicture;
        }

        public async Task<object?> PostSubmissionAsync(Guid userId, SubmissionDto dto, string webRootPath, HttpRequest request)
        {
            Guid? fileId = null;

            if (dto.File is { Length: > 0 })
            {
                await using var ms = new MemoryStream();
                await dto.File.CopyToAsync(ms);
                var bytes = ms.ToArray();

                var uploadsRoot = Path.Combine(webRootPath, "submissions");
                Directory.CreateDirectory(uploadsRoot);

                var originalName = Path.GetFileName(dto.File.FileName);
                var uniqueName = $"{Guid.NewGuid():N}{Path.GetExtension(originalName)}";
                var filePath = Path.Combine(uploadsRoot, uniqueName);

                await File.WriteAllBytesAsync(filePath, bytes);
                var fileUrl = $"{request.Scheme}://{request.Host}/submissions/{uniqueName}";

                var uploaded = new UploadedFile
                {
                    FileId = Guid.NewGuid(),
                    FileName = originalName,
                    ContentType = dto.File.ContentType ?? "application/octet-stream",
                    FileUrl = fileUrl,
                    Content = bytes,
                    UploadedOn = DateTime.UtcNow
                };

                await _repository.SaveUploadedFileAsync(uploaded);
                    fileId = uploaded.FileId;

                // Repository method for saving uploaded file would be needed
            }

            var submission = new Submission
            {
                AssignmentId = dto.AssignmentId,
                UserId = userId,
                CourseId = dto.CourseId,
                WrittenSubmission = dto.WrittenSubmission,
                SubmittedAt = dto.SubmittedAt,
                FileId = fileId
            };

            await _repository.SaveSubmissionAsync(submission);
            return submission;
        }

        public async Task<UploadedFile?> GetUploadedFileByIdAsync(Guid fileId)
        {
            return await _repository.GetUploadedFileByIdAsync(fileId);
        }

         public Task<List<EnrollmentResultDto>> GetMyResultsAsync(Guid userId) =>
          _repository.GetStudentResultsAsync(userId);

        
    }
}
