using EMS.Data;
using EMS.Models;
using EMS.Models.ViewModels.ClassSchedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EMS.Controllers
{
    [Authorize(Roles = "Admin")] // শুধু অ্যাডমিন এক্সেস পাবে
    public class ClassRoutineController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassRoutineController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ClassRoutine
        public async Task<IActionResult> Index(int? departmentId, int? semesterId, string teacherId, DayOfWeekEnum? day)
        {
            // বেসিক কুয়েরি (সব রিলেশনশিপ সহ)
            var routinesQuery = _context.ClassRoutines
                .Include(c => c.Course)
                    .ThenInclude(co => co.Department)
                .Include(c => c.Course)
                    .ThenInclude(co => co.Semester)
                .Include(c => c.Course)
                    .ThenInclude(co => co.Teacher)
                .AsQueryable();

            // ফিল্টার লজিক

            // ডিপার্টমেন্ট ফিল্টার
            if (departmentId.HasValue)
            {
                routinesQuery = routinesQuery.Where(r => r.Course.DepartmentId == departmentId);
            }

            // সেমিস্টার ফিল্টার
            if (semesterId.HasValue)
            {
                routinesQuery = routinesQuery.Where(r => r.Course.SemesterId == semesterId);
            }

            // টিচার ফিল্টার
            if (!string.IsNullOrEmpty(teacherId))
            {
                routinesQuery = routinesQuery.Where(r => r.Course.TeacherId == teacherId);
            }

            // বার (Day) ফিল্টার
            if (day.HasValue)
            {
                routinesQuery = routinesQuery.Where(r => r.Day == day);
            }

            // ড্রপডাউন লিস্টের জন্য ডেটা লোড
            ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", departmentId);
            ViewBag.SemesterId = new SelectList(await _context.Semesters.ToListAsync(), "Id", "Name", semesterId);

            // টিচার লিস্ট
            var teachers = await _context.Users
                .Include(u => u.TeacherProfile)
                .Where(u => u.TeacherProfile != null)
                .Select(u => new {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName
                })
                .ToListAsync();

            ViewBag.TeacherId = new SelectList(teachers, "Id", "FullName", teacherId);


            // সাজানো এবং রিটার্ন (বার এবং সময় অনুযায়ী)
            var routines = await routinesQuery
                .OrderBy(r => r.Day)
                .ThenBy(r => r.StartTime)
                .ToListAsync();

            return View(routines);
        }

        // GET: ClassRoutine/Create
        public IActionResult Create()
        {
            var model = new ClassRoutineViewModel();

            // কোর্সের লিস্ট ড্রপডাউনের জন্য (CourseCode - Title)
            var courses = _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Select(c => new
                {
                    Id = c.Id,
                    Title = $"{c.CourseCode}: {c.Title} ({c.Department.Name} - {c.Semester.Name})"
                })
                .ToList();

            model.CourseList = new SelectList(courses, "Id", "Title");

            return View(model);
        }

        // POST: ClassRoutine/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClassRoutineViewModel model)
        {
            if (ModelState.IsValid)
            {
                // ViewModel থেকে মেইন মডেলে কনভার্ট করা
                var routine = new ClassRoutine
                {
                    CourseId = model.CourseId,
                    Day = model.Day,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    RoomNo = model.RoomNo
                };

                _context.Add(routine);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Routine added successfully!";
                return RedirectToAction(nameof(Index));
            }

            // Error হলে ড্রপডাউন আবার লোড করো
            var courses = _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Select(c => new
                {
                    Id = c.Id,
                    Title = $"{c.CourseCode}: {c.Title} ({c.Department.Name} - {c.Semester.Name})"
                })
                .ToList();

            model.CourseList = new SelectList(courses, "Id", "Title", model.CourseId);
            return View(model);
        }

        // GET: ClassRoutine/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var routine = await _context.ClassRoutines
                .Include(c => c.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (routine == null) return NotFound();

            return View(routine);
        }

        // POST: ClassRoutine/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var routine = await _context.ClassRoutines.FindAsync(id);
            if (routine != null)
            {
                _context.ClassRoutines.Remove(routine);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}