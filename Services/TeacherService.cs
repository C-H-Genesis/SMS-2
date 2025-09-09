using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DTOs;
using Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services
{
    public class TeacherService : ITeacherService
    {
        private readonly ITeacherRepository _repository;

        public TeacherService(ITeacherRepository repository)
        {
            _repository = repository;
        }

        public async Task<ProfileDto?> GetProfileAsync(Guid teacherId)
        {
            var user = await _repository.GetUserByIdAsync(teacherId);
            if (user == null) return null;

            return new ProfileDto
            {
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid teacherId, UpdateProfileDto dto)
        {
            var user = await _repository.GetUserByIdAsync(teacherId);
            if (user == null) return false;

            user.Username = dto.UserName?.Trim() ?? user.Username;
            user.FullName = dto.FullName?.Trim() ?? user.FullName;
            user.Email = dto.Email?.Trim() ?? user.Email;
            user.PhoneNumber = dto.PhoneNumber?.Trim() ?? user.PhoneNumber;

            await _repository.SaveAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid teacherId, ChangePasswordRequest request)
        {
            var user = await _repository.GetUserByIdAsync(teacherId);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _repository.SaveAsync();
            return true;
        }

        public async Task<IEnumerable<Course>> GetCoursesAsync(Guid teacherId)
        {
            return await _repository.GetCoursesByTeacherAsync(teacherId);
        }

        public async Task<IEnumerable<StudentDto>> GetStudentsByCourseAsync(string courseCode)
        {
            var enrollments = await _repository.GetEnrollmentsByCourseCodeAsync(courseCode);

            return enrollments
                .Where(e => e.User is Student)
                .Select(e => new StudentDto
                {
                    FullName = e.User?.FullName ?? "Unknown",
                    RegNumber = (e.User as Student)?.RegNumber ?? "N/A"
                });
        }

        public async Task<UploadedFileDto?> UploadAssignmentFileAsync(IFormFile file, HttpRequest request, string webRootPath)
        {
            if (file == null || file.Length == 0) return null;

            byte[] fileBytes;
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            fileBytes = ms.ToArray();

            var uploadsPath = Path.Combine(webRootPath ?? "", "uploads");
            Directory.CreateDirectory(uploadsPath);

            var originalName = Path.GetFileName(file.FileName);
            var uniqueName = $"{Guid.NewGuid():N}{Path.GetExtension(originalName)}";
            var filePath = Path.Combine(uploadsPath, uniqueName);
            await File.WriteAllBytesAsync(filePath, fileBytes);

            var fileUrl = $"{request.Scheme}://{request.Host}/uploads/{uniqueName}";

            var uploaded = new UploadedFile
            {
                FileId = Guid.NewGuid(),
                FileName = originalName,
                ContentType = file.ContentType ?? "application/octet-stream",
                FileUrl = fileUrl,
                Content = fileBytes,
                UploadedOn = DateTime.UtcNow
            };

            _repository.Context.UploadedFiles.Add(uploaded);
            await _repository.SaveAsync();

            return new UploadedFileDto
            {
                FileId = uploaded.FileId,
                FileUrl = uploaded.FileUrl
            };
        }

        public async Task<AssignmentDto> CreateAssignmentAsync(AssignmentDto dto, Guid teacherId)
        {
            var assignment = new Assignments
            {
                Title = dto.Title,
                Description = dto.Description,
                WrittenAssignment = dto.WrittenAssignment,
                CreatedAt = DateTime.UtcNow,
                DueDate = dto.DueDate,
                CourseId = dto.CourseId,
                TeacherId = teacherId,
                FileId = dto.FileId
            };

            _repository.Context.Assignment.Add(assignment);
            await _repository.SaveAsync();

            dto.Id = assignment.Id;
            return dto;
        }

        public async Task<IEnumerable<AssignmentDto>> GetAllAssignmentsAsync(Guid teacherId)
        {
            var assignments = await _repository.GetAssignmentsByTeacherIdAsync(teacherId);

            return assignments.Select(a => new AssignmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                DueDate = a.DueDate,
                CreatedAt = a.CreatedAt,
                WrittenAssignment = a.WrittenAssignment,
                CourseId = a.CourseId,
                CourseName = a.Course?.CourseName,
                FileId = a.FileId
            });
        }

        public async Task<IEnumerable<SubmissionForTeacherDto>> GetAllSubmissionsAsync(Guid teacherId)
        {
            var submissions = await _repository.GetAllSubmissionsByTeacherAsync(teacherId);
            return submissions.Select(ToSubmissionDto);
        }

        public async Task<IEnumerable<SubmissionForTeacherDto>> GetSubmissionsByCourseAsync(Guid teacherId, Guid courseId)
        {
            var ownsCourse = await _repository.CourseBelongsToTeacherAsync(courseId, teacherId);
            if (!ownsCourse) return Enumerable.Empty<SubmissionForTeacherDto>();

            var submissions = await _repository.GetSubmissionsByCourseIdAsync(courseId);
            return submissions.Select(ToSubmissionDto);
        }

        public async Task<Submission?> GetSubmissionByIdAsync(Guid id)
        {
            return await _repository.GetSubmissionByIdAsync(id);
        }

        public async Task<bool> UpdateSubmissionAsync(Guid id, SubmissionDto dto)
        {
            var submission = await _repository.GetSubmissionByIdAsync(id);
            if (submission == null) return false;

            submission.AssignmentId = dto.AssignmentId;
            submission.CourseId = dto.CourseId;
            submission.UserId = dto.UserId;
            submission.SubmittedAt = dto.SubmittedAt;
            if (dto.FileId.HasValue)
                submission.FileId = dto.FileId;

            await _repository.SaveAsync();
            return true;
        }

        public async Task<bool> DeleteSubmissionAsync(Guid id)
        {
            var submission = await _repository.GetSubmissionByIdAsync(id);
            if (submission == null) return false;

            _repository.Context.Submission.Remove(submission);
            await _repository.SaveAsync();
            return true;
        }

        public async Task<string> GradeSubmissionAsync(Guid submissionId, GradeDto dto, Guid teacherId)
        {
            // 1) Validate
            var submission = await _repository.GetSubmissionByIdAsync(submissionId);
            if (submission == null || submission.Assignment?.TeacherId != teacherId)
                return "Not allowed or submission not found.";

            // 2) Prevent double‐grading
            if (_repository.Context.Grade.Any(g => g.SubmissionId == submissionId))
                return "Already graded.";

            var enrollment = await _repository.GetEnrollmentAsync(
                submission.UserId, submission.CourseId)
                ?? throw new InvalidOperationException("Enrollment not found.");    

            // 3) Create the grade row
            var grade = new Grades
            {
                Id           = Guid.NewGuid(),
                UserId       = submission.UserId,
                CourseId     = submission.CourseId,
                SubmissionId = submissionId,
                EnrollmentId = enrollment.Id, 
                Score        = dto.Score,
                GradeText    = ConvertScoreToLetter(dto.Score),
                Feedback     = dto.Feedback,
                GradedAt     = DateTime.UtcNow
            };
            _repository.Context.Grade.Add(grade);
            await _repository.SaveAsync();

            // 4) Recalculate enrollment average & letter
            await RecalculateEnrollmentResultAsync(submission.UserId, submission.CourseId);

            return "Graded successfully.";
        }

        public async Task<string> UpdateSubmissionGradeAsync(Guid submissionId, GradeDto dto, Guid teacherId)
        {
            // 1) Validate
            var submission = await _repository.GetSubmissionByIdAsync(submissionId);
            if (submission == null || submission.Assignment?.TeacherId != teacherId)
                return "Not allowed.";

            // 2) Find existing grade
            var grade = _repository.Context.Grade.FirstOrDefault(g => g.SubmissionId == submissionId);
            if (grade == null)
                return "Grade not found.";

            // 3) Update fields
            grade.Score     = dto.Score;
            grade.GradeText = ConvertScoreToLetter(dto.Score);
            grade.Feedback  = dto.Feedback;
            grade.GradedAt  = DateTime.UtcNow;
            await _repository.SaveAsync();

            // 4) Recalculate enrollment average & letter
            await RecalculateEnrollmentResultAsync(submission.UserId, submission.CourseId);

            return "Grade updated.";
        }

        private async Task RecalculateEnrollmentResultAsync(Guid userId, Guid courseId)
        {
            // A) Load all grades for this student/course
            var allGrades = await _repository.Context.Grade
                .Where(g => g.UserId == userId && g.CourseId == courseId)
                .ToListAsync();

            // B) Compute average (or null if none)
            double? avg = allGrades.Count == 0
                ? (double?)null
                : allGrades.Average(g => g.Score);

            // C) Map to letter
            string? letter = avg.HasValue
                ? ConvertScoreToLetter((int)Math.Round(avg.Value))
                : null;

            // D) Fetch and update the enrollment
            var enrollment = await _repository.Context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

            if (enrollment != null)
            {
                enrollment.AverageScore = avg;
                enrollment.LetterGrade  = letter;
                await _repository.SaveAsync();
            }
        }

        public async Task<UploadedFile?> GetUploadedFileByIdAsync(Guid fileId)
        {
            return await _repository.GetUploadedFileAsync(fileId);
        }

        private SubmissionForTeacherDto ToSubmissionDto(Submission s)
        {
            var student = s.User as Student;

            return new SubmissionForTeacherDto
            {
                SubmissionId = s.Id,
                AssignmentId = s.AssignmentId,
                AssignmentTitle = s.Assignment?.Title ?? "",
                CourseName = s.Assignment?.Course?.CourseName ?? "",
                StudentId = s.UserId,
                StudentName = s.User?.FullName ?? "",
                RegNumber = student?.RegNumber ?? "",
                SubmittedAt = s.SubmittedAt,
                WrittenSubmission = s.WrittenSubmission,
                FileId = s.FileId,
                FileName = s.UploadedFile?.FileName,
                ContentType = s.UploadedFile?.ContentType,
                Score = s.Grade?.Score,
                Feedback = s.Grade?.Feedback
            };
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
    }
}
