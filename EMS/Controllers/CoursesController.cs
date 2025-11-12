using EMS.Data;
using EMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Couses এর জন্য Index অ্যাকশন মেথডে সার্চ এবং ফিল্টারিং লজিক
        // GET: Courses
        public async Task<IActionResult> Index(string searchString, int? departmentId, int? semesterId)
        {
            // ১. বেসিক কুয়েরি তৈরি (সব রিলেশনশিপসহ)
            var coursesQuery = _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Include(c => c.Teacher) // টিচারের নাম দিয়ে সার্চ করার জন্য এটা লাগবে
                .AsQueryable();

            // ২. সার্চ লজিক (কোর্সের নাম, কোড অথবা টিচারের নাম দিয়ে সার্চ)
            if (!string.IsNullOrEmpty(searchString))
            {
                coursesQuery = coursesQuery.Where(c =>
                    c.Title.Contains(searchString) ||
                    c.CourseCode.Contains(searchString) ||
                    (c.Teacher != null && (c.Teacher.FirstName.Contains(searchString) || c.Teacher.LastName.Contains(searchString)))
                );
            }

            // ৩. ডিপার্টমেন্ট ফিল্টার
            if (departmentId.HasValue)
            {
                coursesQuery = coursesQuery.Where(c => c.DepartmentId == departmentId);
            }

            // ৪. সেমিস্টার ফিল্টার
            if (semesterId.HasValue)
            {
                coursesQuery = coursesQuery.Where(c => c.SemesterId == semesterId);
            }

            // ৫. ড্রপডাউনগুলোর জন্য ডেটা লোড করা (ফিল্টার ভ্যালু মনে রাখার জন্য)
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", departmentId);
            ViewData["SemesterId"] = new SelectList(_context.Semesters, "Id", "Name", semesterId);

            // সার্চ ভ্যালু মনে রাখার জন্য
            ViewData["CurrentFilter"] = searchString;

            // ৬. ফাইনাল রেজাল্ট
            return View(await coursesQuery.ToListAsync());
        }


        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "FullName");
            ViewData["SemesterId"] = new SelectList(_context.Semesters, "Id", "Name");
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CourseCode,Title,Credits,DepartmentId,SemesterId,TeacherId")] Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "FullName", course.DepartmentId);
            ViewData["SemesterId"] = new SelectList(_context.Semesters, "Id", "Name", course.SemesterId);
            return View(course);
        }

        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "FullName", course.DepartmentId);
            ViewData["SemesterId"] = new SelectList(_context.Semesters, "Id", "Name", course.SemesterId);
            return View(course);
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CourseCode,Title,Credits,DepartmentId,SemesterId,TeacherId")] Course course)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "FullName", course.DepartmentId);
            ViewData["SemesterId"] = new SelectList(_context.Semesters, "Id", "Name", course.SemesterId);
            return View(course);
        }

        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }



        // GET: Courses/AssignTeacher/5
        public async Task<IActionResult> AssignTeacher(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Include(c => c.Teacher) // বর্তমান টিচারকে লোড করো
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            // শুধুমাত্র ওই ডিপার্টমেন্টের টিচারদের লিস্ট তৈরি করো
            var teachers = await _context.Users
                .Include(u => u.TeacherProfile)
                .Where(u => u.TeacherProfile != null && u.TeacherProfile.DepartmentId == course.DepartmentId) // লজিক: অন্য ডিপার্টমেন্টের টিচার অ্যাসাইন করা যাবে না
                .Select(u => new {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName + " (" + u.TeacherProfile.Designation + ")"
                })
                .ToListAsync();

            ViewData["TeacherId"] = new SelectList(teachers, "Id", "Name", course.TeacherId);

            return View(course);
        }


        // POST: Courses/AssignTeacher/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTeacher(int id, string? TeacherId)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            // টিচার আপডেট করো
            course.TeacherId = TeacherId;

            _context.Update(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Teacher assigned successfully!";
            return RedirectToAction(nameof(Index));
        }


        // AJAX-এর মাধ্যমে টিচার লোড করার জন্য API
        public async Task<JsonResult> GetTeachersByDepartment(int departmentId)
        {
            var teachers = await _context.Users
                .Include(u => u.TeacherProfile)
                .Where(u => u.TeacherProfile != null && u.TeacherProfile.DepartmentId == departmentId)
                .Select(u => new {
                    id = u.Id,
                    name = u.FirstName + " " + u.LastName + " (" + u.TeacherProfile.Designation + ")"
                })
                .ToListAsync();

            return Json(teachers);
        }
    }
}
