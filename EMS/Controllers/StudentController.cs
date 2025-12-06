using EMS.Data;
using EMS.Models;
using EMS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using UglyToad.PdfPig; // PDF পড়ার জন্য
using System.Text;     // টেক্সট প্রসেসিং
using System.Net.Http; // ইন্টারনেটে রিকোয়েস্ট পাঠানোর জন্য
using System.Text.Json; // JSON ডেটা হ্যান্ডেল করার জন্য
using System.Text.Json.Nodes; // JSON থেকে ডেটা বের করার জন্য

namespace EMS.Controllers
{
    [Authorize(Roles = "Student")] // Only Student Allowed
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context; // ডাটাবেস কনটেক্সট
        private readonly UserManager<ApplicationUser> _userManager; // ইউজার ম্যানেজমেন্টের জন্য
        private readonly IWebHostEnvironment _webHostEnvironment; // ওয়েব হোস্ট এনভায়রনমেন্ট, ফাইল আপলোডের জন্য
        private readonly IConfiguration _configuration; // কনফিগারেশন সেটিংসের জন্য

        public StudentController(ApplicationDbContext context,UserManager<ApplicationUser> userManager,IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment; // যদি ভবিষ্যতে ফাইল আপলোড বা ডাউনলোডের প্রয়োজন হয়
            _configuration = configuration; // কনফিগারেশন সেটিংসের জন্য
        }

        public async Task<IActionResult> Index()
        {
            // লগইন করা স্টুডেন্টের আইডি
            var userId = _userManager.GetUserId(User);

            // স্টুডেন্টের প্রোফাইল তথ্য লোড
            var student = await _context.Users
                .Include(u => u.StudentProfile)
                    .ThenInclude(sp => sp.Department)
                .Include(u => u.StudentProfile)
                    .ThenInclude(sp => sp.Semester)
                .FirstOrDefaultAsync(u => u.Id == userId);

            // স্টুডেন্টদের জন্য নোটিশগুলো লোড
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

            // স্টুডেন্টের প্রোফাইল থেকে তার ডিপার্টমেন্ট ও সেমিস্টার জানো
            var student = await _context.Users
                .Include(u => u.StudentProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (student == null || student.StudentProfile == null)
            {
                return NotFound();
            }

            // সেই ডিপার্টমেন্ট ও সেমিস্টারের কোর্সগুলো খুঁজে (সাথে টিচারের নাম)
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

            // এই স্টুডেন্টের সব অ্যাটেনডেন্স রেকর্ড
            var records = await _context.StudentAttendances
                .Include(a => a.Course)
                .Where(a => a.StudentId == userId)
                .ToListAsync();

            // কোর্স অনুযায়ী গ্রুপ করে রিপোর্ট তৈরি
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

            // এই স্টুডেন্টের সব রেজাল্ট লোড(সাথে এক্সাম এবং কোর্স ইনফো)
            var results = await _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Course)
                .Where(r => r.StudentId == userId)
                .OrderByDescending(r => r.Exam.ExamDate) // লেটেস্ট এক্সাম আগে
                .ToListAsync();

            // ViewModel-এ ম্যাপ
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
        // GET: Student/Assignments
        public async Task<IActionResult> Assignments()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Users.Include(u => u.StudentProfile).FirstOrDefaultAsync(u => u.Id == userId);

            if (student?.StudentProfile == null) return NotFound();

            // অ্যাসাইনমেন্টগুলো লোড
            var assignments = await _context.Assignments
                .Include(a => a.Course)
                .Where(a => a.Course.DepartmentId == student.StudentProfile.DepartmentId &&
                            a.Course.SemesterId == student.StudentProfile.SemesterId)
                .ToListAsync();

            // সাবমিশনগুলো লোড 
            var mySubmissions = await _context.AssignmentSubmissions
                .Where(s => s.StudentId == userId)
                .ToListAsync();

            ViewBag.MySubmissions = mySubmissions;

            // সাজানোর লজিক (Sorting Logic)
            var sortedAssignments = assignments
                .OrderBy(a =>
                {
                    bool isSubmitted = mySubmissions.Any(s => s.AssignmentId == a.Id);
                    bool isExpired = a.Deadline < DateTime.Now;

                    // লজিক: 
                    // - যদি সাবমিট করা হয় = ১ (নিচে যাবে)
                    // - যদি সাবমিট না করা হয় কিন্তু মেয়াদ শেষ = ২ (সবার শেষে যাবে)
                    // - যদি সাবমিট না করা হয় এবং মেয়াদ থাকে = ০ (সবার উপরে থাকবে)

                    if (isSubmitted) return 1;
                    if (isExpired) return 2;
                    return 0; // Active Pending
                })
                .ThenByDescending(a => a.CreatedDate) // এরপর নতুন অ্যাসাইনমেন্ট আগে দেখাবে
                .ToList();

            return View(sortedAssignments);
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

            // ফাইল চেক
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a valid file.");
                var assignment = await _context.Assignments.Include(a => a.Course).FirstOrDefaultAsync(a => a.Id == id);
                return View(assignment);
            }

            // ফাইল সেভ করার ফোল্ডার পাথ তৈরি (wwwroot/uploads/assignments)
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "assignments");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // ইউনিক ফাইলের নাম তৈরি (যাতে নাম কনফ্লিক্ট না হয়)
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // ফাইল কপি করা
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // --- AI Integration
            double? aiMarks = null;
            string? aiReview = null;
            double? aiPlagiarism = null;

            // শুধু PDF ফাইল হলে AI চেক করবে
            if (Path.GetExtension(file.FileName).ToLower() == ".pdf")
            {
                // টেক্সট বের করো
                string pdfContent = ExtractTextFromPdf(filePath); // filePath
                var assignment = await _context.Assignments.FindAsync(id);

                // যদি পর্যাপ্ত টেক্সট থাকে, তবে জেমিনিকে পাঠাও
                if (!string.IsNullOrEmpty(pdfContent) && pdfContent.Length > 50)
                {
                    var aiResult = await GetGeminiAnalysis(pdfContent, assignment.Title, assignment.TotalMarks);

                    aiMarks = aiResult.marks;
                    aiReview = aiResult.feedback;
                    aiPlagiarism = aiResult.ai_probability; // প্লাগিয়ারিজম স্কোর
                }
                else
                {
                    aiReview = "File content too short or unreadable for AI.";
                }
            }



            // ৫. ডেটাবেসে সেভ
            var submission = new AssignmentSubmission
            {
                AssignmentId = id,
                StudentId = _userManager.GetUserId(User),
                FileUrl = "/uploads/assignments/" + uniqueFileName,
                SubmissionDate = DateTime.Now,

                // AI ডেটা
                AIMarks = aiMarks,
                AIReview = aiReview,
                AIPlagiarismScore = aiPlagiarism
            };

            _context.AssignmentSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Assignment submitted successfully!";
            return RedirectToAction(nameof(Assignments));
        }

        // AI ভিত্তিক গ্রেডিং এবং রিভিউ ফাংশন 
        // পিডিএফ থেকে লেখা বের করার ফাংশন
        private string ExtractTextFromPdf(string filePath)
        {
            try
            {
                using (var pdf = PdfDocument.Open(filePath))
                {
                    var sb = new StringBuilder();
                    foreach (var page in pdf.GetPages())
                    {
                        sb.Append(page.Text + " ");
                    }
                    return sb.ToString();
                }
            }
            catch { return ""; }
        }

        // জেমিনি (AI) Call ফাংশন
        
        private class AIResultDTO
        {
            public double marks { get; set; }
            public string feedback { get; set; }
            public double ai_probability { get; set; }
        }

        private async Task<AIResultDTO> GetGeminiAnalysis(string studentAnswer, string assignmentTitle, double totalMarks)
        {
            // appsettings.json থেকে Key এবং URL নেওয়া
            string apiKey = _configuration["Gemini:ApiKey"];
            string baseUrl = _configuration["Gemini:ModelUrl"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(baseUrl))
            {
                return new AIResultDTO { marks = 0, feedback = "Configuration Error: API Key or Model URL missing.", ai_probability = 0 };
            }

            // URL এর সাথে Key যুক্ত করা
            string apiUrl = $"{baseUrl}?key={apiKey}";

            var prompt = $@"
            Act as a strict university professor.
            Assignment Title: {assignmentTitle}
            Total Marks: {totalMarks}
            
            Student's Submission (extracted text):
            ---
            {studentAnswer}
            ---
            
            Your Task:
            1. Grade the submission out of {totalMarks}.
            2. Provide a short feedback (max 3 sentences).
            3. Analyze the writing style for AI generation probability (0-100).
            
            Return ONLY a JSON object in this format (no markdown):
            {{ ""marks"": 0, ""feedback"": ""text"", ""ai_probability"": 0 }}
        ";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            try
            {
                using (var client = new HttpClient())
                {
                    var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(apiUrl, jsonContent);

                    var resultJson = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return new AIResultDTO
                        {
                            marks = 0,
                            feedback = $"API Error: {response.StatusCode}. Check Key/Model.",
                            ai_probability = 0
                        };
                    }

                    var jsonNode = JsonNode.Parse(resultJson);
                    var aiText = jsonNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                    if (string.IsNullOrEmpty(aiText))
                    {
                        return new AIResultDTO { marks = 0, feedback = "AI returned empty response.", ai_probability = 0 };
                    }

                    aiText = aiText.Replace("```json", "").Replace("```", "").Trim();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    return JsonSerializer.Deserialize<AIResultDTO>(aiText, options)
                           ?? new AIResultDTO { marks = 0, feedback = "JSON Parsing Error.", ai_probability = 0 };
                }
            }
            catch (Exception ex)
            {
                return new AIResultDTO
                {
                    marks = 0,
                    feedback = $"System Error: {ex.Message}",
                    ai_probability = 0
                };
            }
        }

        // GET: Student/MyClassRoutine
        public async Task<IActionResult> MyClassRoutine()
        {
            var userId = _userManager.GetUserId(User);

            // স্টুডেন্টের প্রোফাইল
            var student = await _context.Users
                .Include(u => u.StudentProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (student?.StudentProfile == null) return NotFound();

            // স্টুডেন্টের ডিপার্টমেন্ট ও সেমিস্টার অনুযায়ী রুটিন লোড
            var routines = await _context.ClassRoutines
                .Include(c => c.Course)
                    .ThenInclude(co => co.Teacher) // টিচারের নাম দেখার জন্য
                .Where(c => c.Course.DepartmentId == student.StudentProfile.DepartmentId &&
                            c.Course.SemesterId == student.StudentProfile.SemesterId)
                .OrderBy(c => c.Day)        // বার অনুযায়ী সাজানো
                .ThenBy(c => c.StartTime)   // সময় অনুযায়ী সাজানো
                .ToListAsync();

            return View(routines);
        }


        // GET: Student/EvaluationList
        public async Task<IActionResult> EvaluationList()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Users.Include(u => u.StudentProfile).FirstOrDefaultAsync(u => u.Id == userId);

            if (student?.StudentProfile == null) return NotFound();

            // ১. স্টুডেন্টের এনরোল করা কোর্সগুলো বের করো (যেখানে টিচার অ্যাসাইন করা আছে)
            var courses = await _context.Courses
                .Include(c => c.Teacher)
                .Where(c => c.DepartmentId == student.StudentProfile.DepartmentId &&
                            c.SemesterId == student.StudentProfile.SemesterId &&
                            c.TeacherId != null)
                .ToListAsync();

            // ২. স্টুডেন্ট ইতিমধ্যে কাদের রিভিউ দিয়েছে তা চেক করো
            var givenEvaluations = await _context.TeacherEvaluations
                .Where(e => e.StudentId == userId)
                .ToListAsync();

            // ৩. ViewModel এ কনভার্ট করো
            var model = courses.Select(c => new EMS.Models.ViewModels.TeacherEvaluationViewModel
            {
                CourseId = c.Id,
                CourseCode = c.CourseCode,
                CourseTitle = c.Title,
                TeacherId = c.TeacherId,
                TeacherName = $"{c.Teacher.FirstName} {c.Teacher.LastName}",
                IsRated = givenEvaluations.Any(e => e.CourseId == c.Id)
            }).ToList();

            return View(model);
        }

        // GET: Student/RateTeacher/5
        public async Task<IActionResult> RateTeacher(int courseId)
        {
            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null || course.TeacherId == null) return NotFound();

            var model = new EMS.Models.ViewModels.TeacherEvaluationViewModel
            {
                CourseId = course.Id,
                CourseCode = course.CourseCode,
                CourseTitle = course.Title,
                TeacherId = course.TeacherId,
                TeacherName = $"{course.Teacher.FirstName} {course.Teacher.LastName}"
            };

            return View(model);
        }

        // POST: Student/RateTeacher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateTeacher(EMS.Models.ViewModels.TeacherEvaluationViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            // ১. মডেল ভ্যালিডেশন ফিক্স (Read-only ফিল্ডগুলো ইগনোর করা)
            ModelState.Remove("TeacherName");
            ModelState.Remove("CourseCode");
            ModelState.Remove("CourseTitle");

            // ২. ডুপ্লিকেট চেক
            var exists = await _context.TeacherEvaluations
                .AnyAsync(e => e.StudentId == userId && e.CourseId == model.CourseId);

            if (exists)
            {
                TempData["ErrorMessage"] = "You have already rated this teacher for this course.";
                return RedirectToAction(nameof(EvaluationList));
            }

            if (ModelState.IsValid)
            {
                var evaluation = new TeacherEvaluation
                {
                    StudentId = userId,
                    TeacherId = model.TeacherId,
                    CourseId = model.CourseId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    SubmissionDate = DateTime.Now
                };

                _context.TeacherEvaluations.Add(evaluation);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you for your feedback!";
                return RedirectToAction(nameof(EvaluationList));
            }

            // ৩. যদি কোনো এরর থাকে, তবে আবার পেজ দেখাও
            return View(model);
        }
    }
}