using EMS.Data;
using EMS.Models;
using EMS.Models.ViewModels.Teacher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EMS.Controllers
{
    [Authorize(Roles = "Teacher")]
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

            // টিচারের প্রোফাইল লোড
            var teacher = await _context.Users
                .Include(u => u.TeacherProfile)
                    .ThenInclude(tp => tp.Department)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (teacher == null) return NotFound();

            // টিচারের জন্য অ্যাসাইন করা কোর্সগুলো লোড 
            var assignedCourses = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Where(c => c.TeacherId == userId) // শুধু এই টিচারের কোর্স
                .ToListAsync();

            // টিচারদের জন্য নোটিশগুলো লোড (নতুন তারিখ আগে)
            var notices = await _context.Notices
                .Where(n => n.IsForTeachers == true) // শুধু টিচারদের নোটিশ
                .OrderByDescending(n => n.PostedDate)
                .Take(5) // সর্বশেষ ৫টি নোটিশ
                .ToListAsync();

            // ViewModel-এ ডেটা সেট
            var model = new TeacherDashboardViewModel
            {
                Name = $"{teacher.FirstName} {teacher.LastName}",
                Designation = teacher.TeacherProfile?.Designation ?? "N/A",
                Department = teacher.TeacherProfile?.Department?.Name ?? "N/A",
                Email = teacher.Email,
                Phone = teacher.PhoneNumber,

                TotalAssignedCourses = assignedCourses.Count, // কোর্সের সংখ্যা
                AssignedCourses = assignedCourses,             // কোর্সের লিস্ট

                Notices = notices               // নোটিশের লিস্ট
            };

            return View(model);
        }

        //My Couse section showing all assign course.
        // GET: Teacher/MyCourses
        public async Task<IActionResult> MyCourses()
        {
            var userId = _userManager.GetUserId(User);

            // টিচারের অ্যাসাইন করা কোর্সগুলো লোড
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
            // কোর্সটি খুঁজে বের করো (ডিপার্টমেন্ট ও সেমিস্টার জানার জন্য)
            var course = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return NotFound();

            // সিকিউরিটি চেক: এই কোর্সটি কি আসলেই এই টিচারের?
            var userId = _userManager.GetUserId(User);
            if (course.TeacherId != userId)
            {
                return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });
            }

            //লজিক: কোর্সের ডিপার্টমেন্ট ও সেমিস্টারের সাথে মিল থাকা স্টুডেন্টদের খুঁজে 
            var students = await _context.Users
                .Include(u => u.StudentProfile)
                .Where(u => u.StudentProfile != null &&
                            u.StudentProfile.DepartmentId == course.DepartmentId &&
                            u.StudentProfile.SemesterId == course.SemesterId)
                .ToListAsync();

            // কোর্সের তথ্য ভিউ-তে পাঠানোর জন্য ViewBag ব্যবহার
            ViewBag.CourseTitle = course.Title;
            ViewBag.CourseCode = course.CourseCode;
            ViewBag.Semester = course.Semester?.Name;
            ViewBag.Department = course.Department?.Name;

            return View(students);
        }

        //Notice showing in dashboard
        // GET: Teacher/Notices
        public async Task<IActionResult> Notices()
        {
            var notices = await _context.Notices
                .Where(n => n.IsForTeachers == true)
                .OrderByDescending(n => n.PostedDate)
                .ToListAsync();

            return View(notices);
        }

        // GET: Teacher/NoticeDetails/5
        public async Task<IActionResult> NoticeDetails(int? id)
        {
            if (id == null) return NotFound();

            var notice = await _context.Notices.FirstOrDefaultAsync(n => n.Id == id);
            if (notice == null) return NotFound();

            return View(notice);
        }

        //Take Attendance Section for students
        // GET: Teacher/Attendance
        public async Task<IActionResult> Attendance(int? courseId, DateTime? date)
        {
            var userId = _userManager.GetUserId(User);
            var attendanceDate = date ?? DateTime.Now; // তারিখ না দিলে আজকের তারিখ

            // টিচারের সব কোর্স লোড (ড্রপডাউনের জন্য)
            var courses = await _context.Courses
                .Include(c => c.Semester)
                .Where(c => c.TeacherId == userId)
                .ToListAsync();

            // ড্রপডাউন লিস্ট তৈরি
            // আমরা কোর্সের নামের সাথে সেমিস্টারও দেখাচ্ছি যাতে টিচার সহজে চিনতে পারে
            ViewBag.CourseList = new SelectList(courses.Select(c => new {
                Id = c.Id,
                Title = $"{c.Title} ({c.CourseCode}) - {c.Semester?.Name}"
            }), "Id", "Title", courseId);

            // যদি কোনো কোর্স সিলেক্ট করা না থাকে, তবে ফাঁকা ভিউ রিটার্ন
            if (courseId == null)
            {
                return View(new AttendanceViewModel { Date = attendanceDate });
            }

            // কোর্স সিলেক্ট করা থাকলে স্টুডেন্ট লোড
            var selectedCourse = courses.FirstOrDefault(c => c.Id == courseId);
            if (selectedCourse == null) return NotFound();

            var students = await _context.Users
                .Include(u => u.StudentProfile)
                .Where(u => u.StudentProfile != null &&
                            u.StudentProfile.DepartmentId == selectedCourse.DepartmentId &&
                            u.StudentProfile.SemesterId == selectedCourse.SemesterId)
                .OrderBy(u => u.StudentProfile.StudentRoll)
                .ToListAsync();

            // আগের কোনো অ্যাটেনডেন্স আছে কিনা চেক
            var existingAttendance = await _context.StudentAttendances
                .Where(a => a.CourseId == courseId && a.Date.Date == attendanceDate.Date)
                .ToListAsync();

            // ViewModel তৈরি
            var model = new AttendanceViewModel
            {
                CourseId = selectedCourse.Id,
                CourseTitle = selectedCourse.Title,
                CourseCode = selectedCourse.CourseCode,
                Date = attendanceDate,
                Students = students.Select(s => {
                    // যদি আগে অ্যাটেনডেন্স নেওয়া থাকে, সেটা লোড করো
                    var record = existingAttendance.FirstOrDefault(a => a.StudentId == s.Id);
                    return new StudentAttendanceRow
                    {
                        StudentId = s.Id,
                        StudentName = $"{s.FirstName} {s.LastName}",
                        RollNo = s.StudentProfile.StudentRoll,
                        Status = record != null ? record.Status : AttendanceStatus.Present // ডিফল্ট Present
                    };
                }).ToList()
            };

            return View(model);
        }

        // POST: Teacher/Attendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Attendance(AttendanceViewModel model)
        {
            // আগের রেকর্ড ডিলিট  (যাতে এডিট করলে ডুপ্লিকেট না হয়)
            
            var existingRecords = await _context.StudentAttendances
                .Where(a => a.CourseId == model.CourseId && a.Date.Date == model.Date.Date)
                .ToListAsync();

            if (existingRecords.Any())
            {
                _context.StudentAttendances.RemoveRange(existingRecords);
            }

            // নতুন রেকর্ড সেভ
            foreach (var item in model.Students)
            {
                var attendance = new StudentAttendance
                {
                    CourseId = model.CourseId,
                    StudentId = item.StudentId,
                    Date = model.Date,
                    Status = item.Status
                };
                _context.StudentAttendances.Add(attendance);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Attendance saved successfully!";

            // সেভ করার পর ওই পেজে
            return RedirectToAction("Attendance", new { courseId = model.CourseId, date = model.Date });
        }

        //Student list based on department semester and courselist
        // GET: Teacher/MyStudents
        public async Task<IActionResult> MyStudents(int? courseId, int? semesterId)
        {
            var userId = _userManager.GetUserId(User);

            // টিচারের সব কোর্স লোড (ফিল্টার ড্রপডাউনের জন্য)
            var teacherCourses = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Where(c => c.TeacherId == userId)
                .ToListAsync();

            // ড্রপডাউন পপুলেট করা
            ViewBag.CourseList = new SelectList(teacherCourses, "Id", "Title", courseId);

            // সেমিস্টার লিস্ট (শুধু টিচারের কোর্সের সেমিস্টারগুলো)
            var teacherSemesters = teacherCourses.Select(c => c.Semester).DistinctBy(s => s.Id).ToList();
            ViewBag.SemesterList = new SelectList(teacherSemesters, "Id", "Name", semesterId);

            // স্টুডেন্ট খোঁজার লজিক
            // টিচারের কোর্সের ডিপার্টমেন্ট এবং সেমিস্টারের তালিকা নাও
            var allowedDeptSemesters = teacherCourses
                .Select(c => new { c.DepartmentId, c.SemesterId })
                .Distinct()
                .ToList();

            var studentsQuery = _context.Users
                .Include(u => u.StudentProfile)
                    .ThenInclude(sp => sp.Department)
                .Include(u => u.StudentProfile)
                    .ThenInclude(sp => sp.Semester)
                .Where(u => u.StudentProfile != null) // শুধু স্টুডেন্ট
                .AsQueryable();

            // ফিল্টারিং লজিক
            if (courseId.HasValue)
            {
                // যদি নির্দিষ্ট কোর্স সিলেক্ট করা থাকে
                var selectedCourse = teacherCourses.FirstOrDefault(c => c.Id == courseId);
                if (selectedCourse != null)
                {
                    studentsQuery = studentsQuery.Where(s =>
                        s.StudentProfile.DepartmentId == selectedCourse.DepartmentId &&
                        s.StudentProfile.SemesterId == selectedCourse.SemesterId);
                }
            }
            else if (semesterId.HasValue)
            {
                // যদি শুধু সেমিস্টার সিলেক্ট করা থাকে (টিচারের সেই সেমিস্টারের সব কোর্সের স্টুডেন্ট)
                // তবে অবশ্যই টিচারের ডিপার্টমেন্টের হতে হবে
                var validDepts = teacherCourses
                    .Where(c => c.SemesterId == semesterId)
                    .Select(c => c.DepartmentId)
                    .ToList();

                studentsQuery = studentsQuery.Where(s =>
                    s.StudentProfile.SemesterId == semesterId &&
                    validDepts.Contains(s.StudentProfile.DepartmentId));
            }
            else
            {
                // কোনো ফিল্টার না থাকলে: টিচারের সব কোর্সের সব স্টুডেন্ট দেখাও
                // এটি একটু জটিল কুয়েরি, তাই মেমোরিতে এনে ফিল্টার করছি (সহজ করার জন্য)
                var allStudents = await studentsQuery.ToListAsync();

                var myStudents = allStudents.Where(s =>
                    allowedDeptSemesters.Any(x =>
                        x.DepartmentId == s.StudentProfile.DepartmentId &&
                        x.SemesterId == s.StudentProfile.SemesterId
                    )).ToList();

                return View(myStudents);
            }

            return View(await studentsQuery.ToListAsync());
        }

        //Student Details showing in Teacher side
        // GET: Teacher/StudentDetails/string_id
        public async Task<IActionResult> StudentDetails(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var student = await _context.Users
                .Include(u => u.StudentProfile)
                    .ThenInclude(sp => sp.Department)
                .Include(u => u.StudentProfile)
                    .ThenInclude(sp => sp.Semester)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (student == null) return NotFound();

            return View(student);
        }

        // GET: Teacher/ManageExams
        public async Task<IActionResult> ManageExams()
        {
            var userId = _userManager.GetUserId(User);

            // এই টিচারের তৈরি করা সব এক্সাম লোড
            var exams = await _context.Exams
                .Include(e => e.Course)
                    .ThenInclude(c => c.Semester) // সেমিস্টার নাম দেখানোর জন্য
                .Where(e => e.Course.TeacherId == userId)
                .OrderByDescending(e => e.ExamDate)
                .ToListAsync();

            return View(exams);
        }

        // Create Exam Section for Teacher 
        // GET: Teacher/CreateExam
        public async Task<IActionResult> CreateExam()
        {
            var userId = _userManager.GetUserId(User);

            // টিচারের অ্যাসাইন করা কোর্সগুলো লোড (ড্রপডাউনের জন্য)
            var courses = await _context.Courses
                .Include(c => c.Semester)
                .Where(c => c.TeacherId == userId)
                .Select(c => new
                {
                    Id = c.Id,
                    Title = $"{c.CourseCode} : {c.Title} ({c.Semester.Name})"
                })
                .ToListAsync();

            ViewBag.CourseId = new SelectList(courses, "Id", "Title");
            return View();
        }

        // POST: Teacher/CreateExam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExam(Exam exam)
        {
            if (ModelState.IsValid)
            {
                // সেভ করার আগে লজিক (যেমন: তারিখ ভ্যালিডেশন) চেক
                _context.Add(exam);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Exam scheduled successfully!";
                return RedirectToAction(nameof(ManageExams));
            }

            // Error হলে ড্রপডাউন আবার লোড
            var userId = _userManager.GetUserId(User);
            var courses = await _context.Courses
                .Include(c => c.Semester)
                .Where(c => c.TeacherId == userId)
                .Select(c => new
                {
                    Id = c.Id,
                    Title = $"{c.CourseCode} : {c.Title} ({c.Semester.Name})"
                })
                .ToListAsync();

            ViewBag.CourseId = new SelectList(courses, "Id", "Title", exam.CourseId);
            return View(exam);
        }

        // Input Marks Section for Teacher 
        // GET: Teacher/InputMarks/5
        public async Task<IActionResult> InputMarks(int examId)
        {
            // এক্সাম এবং কোর্স তথ্য লোড
            var exam = await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null) return NotFound();

            // সিকিউরিটি চেক (টিচার ভেরিফিকেশন)
            var userId = _userManager.GetUserId(User);
            if (exam.Course.TeacherId != userId) return RedirectToAction("AccessDenied", "Account");

            // স্টুডেন্টদের লোড (যাদের ডিপার্টমেন্ট ও সেমিস্টার মিলে)
            var students = await _context.Users
                .Include(u => u.StudentProfile)
                .Where(u => u.StudentProfile != null &&
                            u.StudentProfile.DepartmentId == exam.Course.DepartmentId &&
                            u.StudentProfile.SemesterId == exam.Course.SemesterId)
                .OrderBy(u => u.StudentProfile.StudentRoll)
                .ToListAsync();

            // আগের কোনো রেজাল্ট আছে কিনা চেক
            var existingResults = await _context.ExamResults
                .Where(r => r.ExamId == examId)
                .ToListAsync();

            // ViewModel তৈরি
            var model = new ExamMarksViewModel
            {
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                CourseTitle = exam.Course.CourseCode,
                TotalMarks = exam.TotalMarks,
                ExamDate = exam.ExamDate,
                Students = students.Select(s => {
                    var result = existingResults.FirstOrDefault(r => r.StudentId == s.Id);
                    return new StudentMarksRow
                    {
                        StudentId = s.Id,
                        StudentName = $"{s.FirstName} {s.LastName}",
                        RollNo = s.StudentProfile.StudentRoll,
                        MarksObtained = result != null ? result.MarksObtained : 0 // আগে মার্কস থাকলে সেটা দেখাও, না থাকলে ০
                    };
                }).ToList()
            };

            return View(model);
        }

        // POST: Teacher/InputMarks
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InputMarks(ExamMarksViewModel model)
        {
            // আগের রেকর্ডগুলো মুছে ফেলো (সিম্পল লজিক: ডিলেট অ্যান্ড ইনসার্ট)
            var existingResults = await _context.ExamResults
                .Where(r => r.ExamId == model.ExamId)
                .ToListAsync();

            if (existingResults.Any())
            {
                _context.ExamResults.RemoveRange(existingResults);
            }

            // নতুন মার্কস সেভ
            foreach (var item in model.Students)
            {
                // ভ্যালিডেশন: মার্কস যেন টোটাল মার্কসের বেশি না হয়
                if (item.MarksObtained > model.TotalMarks)
                {
                    // তুমি চাইলে এখানে এরর হ্যান্ডেল করতে পারো, আমি আপাতত ম্যাক্সিমাম ভ্যালুটাই সেট করে দিচ্ছি
                    item.MarksObtained = model.TotalMarks;
                }

                var result = new ExamResult
                {
                    ExamId = model.ExamId,
                    StudentId = item.StudentId,
                    MarksObtained = item.MarksObtained
                };
                _context.ExamResults.Add(result);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Marks updated successfully!";
            return RedirectToAction(nameof(ManageExams));
        }

        // GET: Teacher/MyClassSchedule
        public async Task<IActionResult> MyClassSchedule(int? semesterId, DayOfWeekEnum? day)
        {
            var userId = _userManager.GetUserId(User);

            // টিচারের রুটিন কুয়েরি তৈরি
            var routinesQuery = _context.ClassRoutines
                .Include(r => r.Course)
                    .ThenInclude(c => c.Department)
                .Include(r => r.Course)
                    .ThenInclude(c => c.Semester)
                .Where(r => r.Course.TeacherId == userId) // শুধু এই টিচারের ক্লাস
                .AsQueryable();

            // ফিল্টার লজিক
            if (semesterId.HasValue)
            {
                routinesQuery = routinesQuery.Where(r => r.Course.SemesterId == semesterId);
            }

            if (day.HasValue)
            {
                routinesQuery = routinesQuery.Where(r => r.Day == day);
            }

            // ফিল্টার ড্রপডাউনের জন্য ডেটা (শুধু টিচারের কোর্সের সেমিস্টারগুলো)
            var teacherSemesters = await _context.Courses
                .Where(c => c.TeacherId == userId)
                .Select(c => c.Semester)
                .Distinct()
                .ToListAsync();

            ViewBag.SemesterId = new SelectList(teacherSemesters, "Id", "Name", semesterId);

            // সাজানো এবং রিটার্ন
            var routines = await routinesQuery
                .OrderBy(r => r.Day)
                .ThenBy(r => r.StartTime)
                .ToListAsync();

            return View(routines);
        }
    }
}