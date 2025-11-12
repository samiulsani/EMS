using EMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Course> Courses { get; set; }

        public DbSet<Semester> Semesters { get; set; }
        // --- নতুন এই দুটি লাইন যোগ করো ---
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<TeacherProfile> TeacherProfiles { get; set; }
        public DbSet<Notice> Notices { get; set; }
        public DbSet<StudentAttendance> StudentAttendances { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
    }
}
