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
    [Authorize(Roles = "Student")] // শুধুমাত্র স্টুডেন্টরা এই কন্ট্রোলারে ঢুকতে পারবে
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

            // --- AI Integration
            double? aiMarks = null;
            string? aiReview = null;
            double? aiPlagiarism = null;

            // শুধু PDF ফাইল হলে AI চেক করবে
            if (Path.GetExtension(file.FileName).ToLower() == ".pdf")
            {
                // ১. টেক্সট বের করো
                string pdfContent = ExtractTextFromPdf(filePath); // filePath হলো যেখানে ফাইল সেভ করেছো
                var assignment = await _context.Assignments.FindAsync(id);

                // ২. যদি পর্যাপ্ত টেক্সট থাকে, তবে জেমিনিকে পাঠাও
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



            // ৫. ডেটাবেসে সেভ করা
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
        // ১. পিডিএফ থেকে লেখা বের করার ফাংশন
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

        // ২. জেমিনি (AI) ডাকার ফাংশন
        // ডিবাগিং-এর জন্য ক্লাস আপডেট
        private class AIResultDTO
        {
            public double marks { get; set; }
            public string feedback { get; set; }
            public double ai_probability { get; set; }
        }

        private async Task<AIResultDTO> GetGeminiAnalysis(string studentAnswer, string assignmentTitle, double totalMarks)
        {
            // ১. তোমার API Key
            string apiKey = "AIzaSyCOqRtW4e5q_c3_L-ZiGB-v3BgmjLh9_Zk";

            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent?key={apiKey}";

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
            
            Return ONLY a JSON object in this format (no markdown, no ```json):
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

                    // ডিবাগিং: যদি এখনো এরর দেয়, আমরা দেখবো
                    if (!response.IsSuccessStatusCode)
                    {
                        return new AIResultDTO
                        {
                            marks = 0,
                            feedback = $"API Error: {response.StatusCode} - {resultJson}",
                            ai_probability = 0
                        };
                    }

                    var jsonNode = JsonNode.Parse(resultJson);
                    var aiText = jsonNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                    if (string.IsNullOrEmpty(aiText))
                    {
                        return new AIResultDTO { marks = 0, feedback = "AI returned empty response.", ai_probability = 0 };
                    }

                    // ক্লিন করা
                    aiText = aiText.Replace("```json", "").Replace("```", "").Trim();

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<AIResultDTO>(aiText, options);

                    return result ?? new AIResultDTO { marks = 0, feedback = "JSON Parsing returned null.", ai_probability = 0 };
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
    }
}