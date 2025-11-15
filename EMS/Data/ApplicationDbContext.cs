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

        public DbSet<ClassRoutine> ClassRoutines { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }


        // Cascade Delete সমস্যা সমাধানের জন্য OnModelCreating ওভাররাইড করো
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // AssignmentSubmission-এর জন্য Cascade Delete বন্ধ করা
            modelBuilder.Entity<AssignmentSubmission>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict); // অথবা DeleteBehavior.NoAction

            // একইভাবে ExamResult এর জন্যও সমস্যা হতে পারে, তাই সেফটির জন্য এটাও দিয়ে রাখতে পারো
            modelBuilder.Entity<ExamResult>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
