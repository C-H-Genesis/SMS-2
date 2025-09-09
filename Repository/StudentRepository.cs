using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationDbContext;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Models;
using DTOs;

namespace Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly SMSDbContext _context;

        public StudentRepository(SMSDbContext context)
        {
            _context = context;
        }

        public async Task<Student?> GetStudentByIdAsync(Guid userId)
        {
            
            var user = await _context.Users.OfType<Student>()
                      .FirstOrDefaultAsync(s => s.UserId == userId);

              return user;  // this is a Users? type
        }

        public async Task<Student?> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
             return user as Student;
        }

        public async Task<IEnumerable<Course>> GetCoursesByFacultyIdAsync(int facultyId)
        {
            return await _context.Courses.Where(c => c.FacultyId == facultyId).ToListAsync();
        }

        public async Task<IEnumerable<Enrollment>> GetEnrollmentsByUserIdAsync(Guid userId)
        {
            return await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignments>> GetAssignmentsForStudentAsync(Guid userId)
        {
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.UserId == userId)
                .Select(e => e.CourseId)
                .ToListAsync();

            return await _context.Assignment
                .Include(a => a.Course)
                .Include(a => a.UploadedFile)
                .Include(a => a.Submissions!
                  .Where(s => s.UserId == userId))
                    .ThenInclude(s => s.Grade)
                .Where(a => enrolledCourseIds.Contains(a.CourseId))
                .ToListAsync();
        }

        public async Task SaveUploadedFileAsync(UploadedFile file)
        {
            _context.UploadedFiles.Add(file);
            await _context.SaveChangesAsync();
        }


        public async Task SaveSubmissionAsync(Submission submission)
        {
            _context.Submission.Add(submission);
            await _context.SaveChangesAsync();
        }

        public async Task<UploadedFile?> GetUploadedFileByIdAsync(Guid id)
        {
            return await _context.UploadedFiles.FindAsync(id);
        }

        public async Task<Course?> GetCourseByCodeAsync(string courseCode)
        {
            return await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseCode == courseCode);
        }
 
        public async Task<List<EnrollmentResultDto>> GetStudentResultsAsync(Guid userId)
        {
            return await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Include(e => e.Course)
                .Select(e => new EnrollmentResultDto {
                    CourseId      = e.CourseId,
                    CourseCode    = e.Course!.CourseCode,
                    CourseName    = e.Course!.CourseName,
                    AverageScore  = e.AverageScore,
                    LetterGrade   = e.LetterGrade,
                    EnrollmentDate= e.EnrollmentDate
                })
                .ToListAsync();
        }

         public Task<Enrollment?> GetEnrollmentAsync(Guid userId, Guid courseId) 
            => _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.UserId == userId && e.CourseId == courseId);

        public Task<List<Grades>> GetGradesByEnrollmentIdAsync(int enrollmentId)
        {
            return _context.Grade
                .Where(g => g.EnrollmentId == enrollmentId)
                .ToListAsync();
        }

        public Task<Enrollment?> GetEnrollmentEntityAsync(Guid userId, Guid CourseId) =>
            _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == CourseId);

        public Task AddEnrollmentAsync(Enrollment e) =>
          _context.Enrollments.AddAsync(e).AsTask();  
 
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<(Guid Id, string CourseName)?> GetCourseInfoByCodeAsync(string courseCode)
        {
            var course = await _context.Courses
                .Where(c => c.CourseCode == courseCode)
                .Select(c => new { c.Id, c.CourseName })
                .FirstOrDefaultAsync();

            if (course == null)
                return null;

            return (course.Id, course.CourseName);
        }
    }
}
