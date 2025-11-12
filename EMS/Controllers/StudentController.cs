using EMS.Data;
using EMS.Models;
using EMS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EMS.Controllers
{
    [Authorize(Roles = "Student")] // শুধুমাত্র স্টুডেন্টরা এই কন্ট্রোলারে ঢুকতে পারবে
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // লগইন করা স্টুডেন্টের আইডি বের করো
            var userId = _userManager.GetUserId(User);

            // স্টুডেন্টের প্রোফাইল তথ্য লোড করো
            var student = await _context.Users
                .Include(u => u.StudentProfile)
                    .ThenInclude(sp => sp.Department)
                .Include(u => u.StudentProfile)
                    .ThenInclude(sp => sp.Semester)
                .FirstOrDefaultAsync(u => u.Id == userId);

            // স্টুডেন্টদের জন্য নোটিশগুলো লোড করো
            var notices = await _context.Notices
                .Where(n => n.IsForStudents == true) // শুধু স্টুডেন্টদের নোটিশ
                .OrderByDescending(n => n.PostedDate)
                .Take(5)
                .ToListAsync();

            ViewBag.Notices = notices;

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        //My Couse section showing all assign course.
        // GET: Student/MyCourses
        public async Task<IActionResult> MyCourses()
        {
            var userId = _userManager.GetUserId(User);

            // ১. স্টুডেন্টের প্রোফাইল থেকে তার ডিপার্টমেন্ট ও সেমিস্টার জানো
            var student = await _context.Users
                .Include(u => u.StudentProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (student == null || student.StudentProfile == null)
            {
                return NotFound(); // অথবা এরর পেজে পাঠাতে পারো
            }

            // ২. সেই ডিপার্টমেন্ট ও সেমিস্টারের কোর্সগুলো খুঁজে বের করো (সাথে টিচারের নাম)
            var myCourses = await _context.Courses
                .Include(c => c.Teacher) // টিচারের নাম দেখানোর জন্য
                .Where(c => c.DepartmentId == student.StudentProfile.DepartmentId &&
                            c.SemesterId == student.StudentProfile.SemesterId)
                .ToListAsync();

            return View(myCourses);
        }

        // Notices section showing all notices for students.
        // GET: Student/Notices
        public async Task<IActionResult> Notices()
        {
            var notices = await _context.Notices
                .Where(n => n.IsForStudents == true)
                .OrderByDescending(n => n.PostedDate)
                .ToListAsync();

            return View(notices);
        }

        // GET: Student/NoticeDetails/5
        public async Task<IActionResult> NoticeDetails(int? id)
        {
            if (id == null) return NotFound();

            var notice = await _context.Notices.FirstOrDefaultAsync(n => n.Id == id);
            if (notice == null) return NotFound();

            return View(notice);
        }


        // Student Attendance Report
        // GET: Student/MyAttendance
        public async Task<IActionResult> MyAttendance()
        {
            var userId = _userManager.GetUserId(User);

            // ১. এই স্টুডেন্টের সব অ্যাটেনডেন্স রেকর্ড আনো
            var records = await _context.StudentAttendances
                .Include(a => a.Course)
                .Where(a => a.StudentId == userId)
                .ToListAsync();

            // ২. কোর্স অনুযায়ী গ্রুপ করে রিপোর্ট তৈরি করো
            var model = records.GroupBy(r => r.Course)
                .Select(g => new StudentAttendanceViewModel
                {
                    CourseCode = g.Key.CourseCode,
                    CourseTitle = g.Key.Title,
                    TotalClasses = g.Count(),
                    Present = g.Count(x => x.Status == AttendanceStatus.Present),
                    Late = g.Count(x => x.Status == AttendanceStatus.Late),
                    Absent = g.Count(x => x.Status == AttendanceStatus.Absent)
                })
                .ToList();

            return View(model);
        }

        // Student Exam Results 
        // GET: Student/MyResults
        public async Task<IActionResult> MyResults()
        {
            var userId = _userManager.GetUserId(User);

            // ১. এই স্টুডেন্টের সব রেজাল্ট লোড করো (সাথে এক্সাম এবং কোর্স ইনফো)
            var results = await _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Course)
                .Where(r => r.StudentId == userId)
                .OrderByDescending(r => r.Exam.ExamDate) // লেটেস্ট এক্সাম আগে
                .ToListAsync();

            // ২. ViewModel-এ ম্যাপ করো
            var model = results.Select(r => new EMS.Models.ViewModels.StudentResultViewModel
            {
                CourseCode = r.Exam.Course.CourseCode,
                CourseTitle = r.Exam.Course.Title,
                ExamTitle = r.Exam.Title,
                ExamDate = r.Exam.ExamDate,
                TotalMarks = r.Exam.TotalMarks,
                MarksObtained = r.MarksObtained
            }).ToList();

            return View(model);
        }
    }
}