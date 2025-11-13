using EMS.Data;
using EMS.Models;
using EMS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace EMS.Controllers
{
    [Authorize(Roles = "Student")] // শুধুমাত্র স্টুডেন্টরা এই কন্ট্রোলারে ঢুকতে পারবে
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public StudentController(ApplicationDbContext context,UserManager<ApplicationUser> userManager,IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment; // যদি ভবিষ্যতে ফাইল আপলোড বা ডাউনলোডের প্রয়োজন হয়
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

        // Student Assignments Section 
        // GET: Student/Assignments
        public async Task<IActionResult> Assignments()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Users.Include(u => u.StudentProfile).FirstOrDefaultAsync(u => u.Id == userId);

            if (student?.StudentProfile == null) return NotFound();

            // স্টুডেন্টের সেমিস্টার এবং ডিপার্টমেন্টের অ্যাসাইনমেন্টগুলো লোড করো
            var assignments = await _context.Assignments
                .Include(a => a.Course)
                .Where(a => a.Course.DepartmentId == student.StudentProfile.DepartmentId &&
                            a.Course.SemesterId == student.StudentProfile.SemesterId)
                .OrderByDescending(a => a.Deadline)
                .ToListAsync();

            // স্টুডেন্ট কোনগুলো সাবমিট করেছে তা জানার জন্য সাবমিশন লিস্টও লোড করছি
            var mySubmissions = await _context.AssignmentSubmissions
                .Where(s => s.StudentId == userId)
                .ToListAsync();

            ViewBag.MySubmissions = mySubmissions; // ভিউতে পাঠানোর জন্য

            return View(assignments);
        }

        // GET: Student/SubmitAssignment/5
        public async Task<IActionResult> SubmitAssignment(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return NotFound();

            return View(assignment);
        }

        // POST: Student/SubmitAssignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAssignment(int id, IFormFile file)
        {
            var userId = _userManager.GetUserId(User);

            // ১. ফাইল চেক
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a valid file.");
                var assignment = await _context.Assignments.Include(a => a.Course).FirstOrDefaultAsync(a => a.Id == id);
                return View(assignment);
            }

            // ২. ফাইল সেভ করার ফোল্ডার পাথ তৈরি (wwwroot/uploads/assignments)
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "assignments");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // ৩. ইউনিক ফাইলের নাম তৈরি (যাতে নাম কনফ্লিক্ট না হয়)
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // ৪. ফাইল কপি করা
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // ৫. ডেটাবেসে সেভ করা
            var submission = new AssignmentSubmission
            {
                AssignmentId = id,
                StudentId = userId,
                FileUrl = "/uploads/assignments/" + uniqueFileName, // ফোল্ডারের রিলেটিভ পাথ
                SubmissionDate = DateTime.Now
            };

            _context.AssignmentSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Assignment submitted successfully!";
            return RedirectToAction(nameof(Assignments));
        }
    }
}