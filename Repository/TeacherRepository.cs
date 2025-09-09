using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationDbContext;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Repositories
{
    public class TeacherRepository : ITeacherRepository
    {
        private readonly SMSDbContext _context;
        public SMSDbContext Context => _context;

        public TeacherRepository(SMSDbContext context)
        {
            _context = context;
        }

        public async Task<Teacher?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users.OfType<Teacher>()
                      .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<Course?> GetCourseByCodeAsync(string courseCode)
        {
            return await _context.Courses
                .Include(c => c.Enrollments)
                .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(c => c.CourseCode == courseCode);
        }

        public async Task<IEnumerable<Course>> GetCoursesByTeacherAsync(Guid teacherId)
        {
            return await _context.Courses
                .Where(c => c.UserId == teacherId)
                .ToListAsync();
        }

        public Task<Enrollment?> GetEnrollmentAsync(Guid userId, Guid courseId)
        {
            return _context.Enrollments
                       .FirstOrDefaultAsync(e =>
                           e.UserId   == userId &&
                           e.CourseId == courseId);
        }

        public async Task<IEnumerable<Enrollment>> GetEnrollmentsByCourseCodeAsync(string courseCode)
        {
            var course = await _context.Courses
                .Include(c => c.Enrollments)
                .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(c => c.CourseCode == courseCode);

            return course?.Enrollments ?? new List<Enrollment>();
        }

        public async Task<bool> CourseBelongsToTeacherAsync(Guid courseId, Guid teacherId)
        {
            return await _context.Courses
                .AnyAsync(c => c.Id == courseId && c.UserId == teacherId);
        }

        public async Task<IEnumerable<Assignments>> GetAssignmentsByTeacherIdAsync(Guid teacherId)
        {
            return await _context.Assignment
                .Where(a => a.TeacherId == teacherId)
                .Include(a => a.Course)
                .Include(a => a.UploadedFile)
                .ToListAsync();
        }

        public async Task<IEnumerable<Submission>> GetAllSubmissionsByTeacherAsync(Guid teacherId)
        {
            return await _context.Submission
                .Include(s => s.Assignment!).ThenInclude(a => a.Course)
                .Include(s => s.User)
                .Include(s => s.UploadedFile)
                .Include(s => s.Grade)
                .Where(s => s.Assignment != null && s.Assignment.TeacherId == teacherId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Submission>> GetSubmissionsByCourseIdAsync(Guid courseId)
        {
            return await _context.Submission
                .Include(s => s.Assignment!).ThenInclude(a => a.Course)
                .Include(s => s.User)
                .Include(s => s.UploadedFile)
                .Include(s => s.Grade)
                .Where(s => s.CourseId == courseId)
                .ToListAsync();
        }

        public async Task<Submission?> GetSubmissionByIdAsync(Guid id)
        {
            return await _context.Submission
                .Include(s => s.Assignment)
                .Include(s => s.User)
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<UploadedFile?> GetUploadedFileAsync(Guid fileId)
        {
            return await _context.UploadedFiles.FindAsync(fileId);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
