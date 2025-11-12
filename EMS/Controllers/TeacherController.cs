using EMS.Data;
using EMS.Models;
using EMS.Models.ViewModels.Teacher; // <--- নতুন ViewModel-এর রেফারেন্স
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EMS.Controllers
{
    [Authorize(Roles = "Teacher")] // শুধু টিচাররা এক্সেস পাবে
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // ১. টিচারের প্রোফাইল লোড করো
            var teacher = await _context.Users
                .Include(u => u.TeacherProfile)
                    .ThenInclude(tp => tp.Department)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (teacher == null) return NotFound();

            // ২. টিচারের জন্য অ্যাসাইন করা কোর্সগুলো লোড করো
            var assignedCourses = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Where(c => c.TeacherId == userId) // শুধু এই টিচারের কোর্স
                .ToListAsync();

            // ৩. ViewModel-এ ডেটা সেট করো
            var model = new TeacherDashboardViewModel
            {
                Name = $"{teacher.FirstName} {teacher.LastName}",
                Designation = teacher.TeacherProfile?.Designation ?? "N/A",
                Department = teacher.TeacherProfile?.Department?.Name ?? "N/A",
                Email = teacher.Email,
                Phone = teacher.PhoneNumber,

                // নতুন ডেটা
                TotalAssignedCourses = assignedCourses.Count, // কোর্সের সংখ্যা
                AssignedCourses = assignedCourses             // কোর্সের লিস্ট
            };

            return View(model);
        }

        //My Couse section showing all assign course.
        // GET: Teacher/MyCourses
        public async Task<IActionResult> MyCourses()
        {
            var userId = _userManager.GetUserId(User);

            // টিচারের অ্যাসাইন করা কোর্সগুলো লোড করো
            var assignedCourses = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Where(c => c.TeacherId == userId) // ফিল্টার: শুধু এই টিচারের কোর্স
                .ToListAsync();

            return View(assignedCourses); // লিস্ট পাঠিয়ে দিলাম
        }

        //Student list based on department semester and courselist
        // GET: Teacher/EnrolledStudents/5
        public async Task<IActionResult> EnrolledStudents(int courseId)
        {
            // ১. কোর্সটি খুঁজে বের করো (ডিপার্টমেন্ট ও সেমিস্টার জানার জন্য)
            var course = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return NotFound();

            // ২. সিকিউরিটি চেক: এই কোর্সটি কি আসলেই এই টিচারের?
            var userId = _userManager.GetUserId(User);
            if (course.TeacherId != userId)
            {
                return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });
            }

            // ৩. লজিক: কোর্সের ডিপার্টমেন্ট ও সেমিস্টারের সাথে মিল থাকা স্টুডেন্টদের খুঁজে বের করো
            var students = await _context.Users
                .Include(u => u.StudentProfile)
                .Where(u => u.StudentProfile != null &&
                            u.StudentProfile.DepartmentId == course.DepartmentId &&
                            u.StudentProfile.SemesterId == course.SemesterId)
                .ToListAsync();

            // ৪. কোর্সের তথ্য ভিউ-তে পাঠানোর জন্য ViewBag ব্যবহার করছি
            ViewBag.CourseTitle = course.Title;
            ViewBag.CourseCode = course.CourseCode;
            ViewBag.Semester = course.Semester?.Name;
            ViewBag.Department = course.Department?.Name;

            return View(students);
        }
    }
}