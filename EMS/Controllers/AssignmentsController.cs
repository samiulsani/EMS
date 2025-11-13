using EMS.Data;
using EMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EMS.Controllers
{
    [Authorize(Roles = "Teacher")] // শুধুমাত্র টিচারদের জন্য
    public class AssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssignmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Assignments (টিচারের সব অ্যাসাইনমেন্ট)
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var assignments = await _context.Assignments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Semester)
                .Where(a => a.TeacherId == userId) // শুধু নিজের তৈরি অ্যাসাইনমেন্ট
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return View(assignments);
        }

        // GET: Assignments/Create
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);

            // টিচারের কোর্সগুলো লোড করো
            var courses = await _context.Courses
                .Include(c => c.Semester)
                .Where(c => c.TeacherId == userId)
                .Select(c => new
                {
                    Id = c.Id,
                    Title = $"{c.CourseCode} - {c.Title} ({c.Semester.Name})"
                })
                .ToListAsync();

            ViewBag.CourseId = new SelectList(courses, "Id", "Title");
            return View();
        }

        // POST: Assignments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Assignment assignment)
        {
            var userId = _userManager.GetUserId(User);

            ModelState.Remove("TeacherId");
            ModelState.Remove("Teacher");
            ModelState.Remove("Course");

            // টিচার আইডি সেট করো
            assignment.TeacherId = userId;
            assignment.CreatedDate = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.Add(assignment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Assignment created successfully!";
                return RedirectToAction(nameof(Index));
            }

            // Error হলে ড্রপডাউন আবার লোড করো
            var courses = await _context.Courses
                .Include(c => c.Semester)
                .Where(c => c.TeacherId == userId)
                .Select(c => new
                {
                    Id = c.Id,
                    Title = $"{c.CourseCode} - {c.Title} ({c.Semester.Name})"
                })
                .ToListAsync();

            ViewBag.CourseId = new SelectList(courses, "Id", "Title", assignment.CourseId);
            return View(assignment);
        }

        // POST: Assignments/Submissions/5 (সাবমিশন মার্ক করার জন্য)
        // GET: Assignments/Submissions/5 (সাবমিশন দেখার জন্য)
        public async Task<IActionResult> Submissions(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignment == null) return NotFound();

            // সিকিউরিটি চেক
            var userId = _userManager.GetUserId(User);
            if (assignment.TeacherId != userId) return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });

            // সাবমিশন লিস্ট লোড করো
            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.Student)
                .ThenInclude(u => u.StudentProfile)
                .Where(s => s.AssignmentId == id)
                .ToListAsync();

            ViewBag.AssignmentTitle = assignment.Title;
            ViewBag.TotalMarks = assignment.TotalMarks;
            return View(submissions);
        }

        // GET: Assignments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null) return NotFound();

            // সিকিউরিটি
            var userId = _userManager.GetUserId(User);
            if (assignment.TeacherId != userId) return Unauthorized();

            return View(assignment);
        }

        // POST: Assignments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Assignment assignment)
        {
            if (id != assignment.Id) return NotFound();

            // আগের কিছু তথ্য ধরে রাখার জন্য (TeacherId, CourseId)
            var existingAssignment = await _context.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (existingAssignment == null) return NotFound();

            // যে ফিল্ডগুলো চেঞ্জ হবে না
            assignment.TeacherId = existingAssignment.TeacherId;
            assignment.CourseId = existingAssignment.CourseId;
            assignment.CreatedDate = existingAssignment.CreatedDate;

            // ভ্যালিডেশন ইগনোর
            ModelState.Remove("Teacher");
            ModelState.Remove("Course");
            ModelState.Remove("TeacherId");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(assignment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Assignments.Any(e => e.Id == assignment.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(assignment);
        }

        // GET: Assignments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignment == null) return NotFound();

            return View(assignment);
        }

        // POST: Assignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment != null)
            {
                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Assignments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignment == null) return NotFound();

            return View(assignment);
        }

        // GET & POST: Assignments/GradeSubmission/5
        // GET: Assignments/GradeSubmission/5
        public async Task<IActionResult> GradeSubmission(int? id)
        {
            if (id == null) return NotFound();

            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .Include(s => s.Student)
                    .ThenInclude(u => u.StudentProfile)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null) return NotFound();

            return View(submission);
        }

        // POST: Assignments/GradeSubmission/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GradeSubmission(int id, double marks, string feedback)
        {
            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null) return NotFound();

            // মার্কস ভ্যালিডেশন
            if (marks < 0 || marks > submission.Assignment.TotalMarks)
            {
                ModelState.AddModelError("", $"Marks must be between 0 and {submission.Assignment.TotalMarks}");
                return View(submission);
            }

            // ডেটা আপডেট
            submission.MarksObtained = marks;
            submission.TeacherFeedback = feedback;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Grading completed successfully!";
            return RedirectToAction("Submissions", new { id = submission.AssignmentId });
        }
    }
}