using EMS.Data;
using EMS.Models;
using EMS.Models.ViewModels;
using EMS.Models.ViewModels.Teacher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context; // <--- ডেটাবেস কনটেক্সট যোগ করো

        [TempData]
        public string StatusMessage { get; set; }

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context; // Initialize the context
        }

        public IActionResult Index()
        {
            ViewBag.StatusMessage = StatusMessage;
            return View();
        }

        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult SharedPage()
        {
            return View();
        }

        // --- CreateUser (GET) ---
        public async Task<IActionResult> CreateUser(string source)
        {
            // ড্রপডাউনের জন্য সব ডেটা লোড
            var roles = await _roleManager.Roles
                .Where(r => r.Name == "Student" || r.Name == "Teacher") // শুধু স্টুডেন্ট বা টিচার রোল
                .ToListAsync();

            var departments = await _context.Departments.ToListAsync();
            var semesters = await _context.Semesters.ToListAsync();

            var model = new CreateUserViewModel
            {
                RoleList = new SelectList(roles, "Name", "Name"),
                DepartmentList = new SelectList(departments, "Id", "Name"),
                SemesterList = new SelectList(semesters, "Id", "Name")
            };

            ViewData["Source"] = source; // যেখানে থেকে ক্রিয়েট পেজে এসেছি
            return View(model);
        }

        // --- CreateUser (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model, string source)
        {
            // ম্যানুয়ালি ভ্যালিডেশন চেক (রোল অনুযায়ী)
            if (model.Role == "Student")
            {
                if (model.SemesterId == null)
                    ModelState.AddModelError("SemesterId", "Semester is required for Students.");
                if (string.IsNullOrEmpty(model.StudentRoll))
                    ModelState.AddModelError("StudentRoll", "Student Roll is required for Students.");
                if (string.IsNullOrEmpty(model.RegistrationNo))
                    ModelState.AddModelError("RegistrationNo", "Registration No is required for Students.");
                if (string.IsNullOrEmpty(model.Session))
                    ModelState.AddModelError("Session", "Session is required for Students.");
                if (model.DateOfBirth == null)
                    ModelState.AddModelError("DateOfBirth", "Date of Birth is required for Students.");
                if (string.IsNullOrEmpty(model.FatherName))
                    ModelState.AddModelError("FatherName", "Father's Name is required for Students.");
                if (string.IsNullOrEmpty(model.MotherName))
                    ModelState.AddModelError("MotherName", "Mother's Name is required for Students.");
                if (string.IsNullOrEmpty(model.Address))
                    ModelState.AddModelError("Address", "Address is required for Students.");
            }
            else if (model.Role == "Teacher")
            {
                if (string.IsNullOrEmpty(model.Designation))
                    ModelState.AddModelError("Designation", "Designation is required for Teachers.");
            }

            if (ModelState.IsValid)
            {
                // ApplicationUser (লগইন)
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = true // আমরা অ্যাডমিন প্যানেল থেকে বানাচ্ছি, তাই কনফার্মড
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // ইউজারকে রোলে অ্যাসাইন
                    await _userManager.AddToRoleAsync(user, model.Role);

                    // রোল অনুযায়ী প্রোফাইল তৈরি
                    if (model.Role == "Student")
                    {
                        var studentProfile = new StudentProfile
                        {
                            Id = user.Id, // 1-to-1 রিলেশনশিপ
                            StudentRoll = model.StudentRoll,
                            RegistrationNo = model.RegistrationNo,
                            Session = model.Session,
                            DateOfBirth = model.DateOfBirth.Value,
                            BloodGroup = model.BloodGroup,
                            FatherName = model.FatherName,
                            MotherName = model.MotherName,
                            Address = model.Address,
                            DepartmentId = model.DepartmentId,
                            SemesterId = model.SemesterId.Value
                        };
                        _context.StudentProfiles.Add(studentProfile);
                    }
                    else if (model.Role == "Teacher")
                    {
                        var teacherProfile = new TeacherProfile
                        {
                            Id = user.Id, // 1-to-1 রিলেশনশিপ
                            Designation = model.Designation,
                            DepartmentId = model.DepartmentId
                        };
                        _context.TeacherProfiles.Add(teacherProfile);
                    }

                    await _context.SaveChangesAsync(); // প্রোফাইল সেভ

                    TempData["SuccessMessage"] = "User created successfully!";

                    // ক্রিয়েট করার পর যেখানে থেকে এসেছি সেখানে রিডাইরেক্ট করো
                    if (source == "Students") return RedirectToAction(nameof(Students));
                    if (source == "Teachers") return RedirectToAction(nameof(Teachers));
                    return RedirectToAction("ListUsers"); // সফল হলে ইউজার লিস্টে পাঠাও
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // --- Error হলে ড্রপডাউনগুলো আবার লোড ---
            var roles = await _roleManager.Roles
                .Where(r => r.Name == "Student" || r.Name == "Teacher")
                .ToListAsync();
            var departments = await _context.Departments.ToListAsync();
            var semesters = await _context.Semesters.ToListAsync();

            model.RoleList = new SelectList(roles, "Name", "Name", model.Role);
            model.DepartmentList = new SelectList(departments, "Id", "Name", model.DepartmentId);
            model.SemesterList = new SelectList(semesters, "Id", "Name", model.SemesterId);

            ViewData["Source"] = source;
            return View(model);
        }

        //Get Details from user. It's for user managment 
        // GET: Admin/Details/5
        public async Task<IActionResult> Details(string id, string source)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.StudentProfile)
                    .ThenInclude(s => s.Department)
                .Include(u => u.StudentProfile)
                    .ThenInclude(s => s.Semester)
                .Include(u => u.TeacherProfile)
                    .ThenInclude(t => t.Department)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            // --- নতুন অংশ: একাডেমিক হিস্ট্রি লোড ---
            if (user.StudentProfile != null)
            {
                // ১. সব রেজাল্ট আনো (সেমিস্টার সহ)
                var results = await _context.ExamResults
                    .Include(r => r.Exam)
                        .ThenInclude(e => e.Course)
                            .ThenInclude(c => c.Semester)
                    .Where(r => r.StudentId == id)
                    .ToListAsync();

                var resultList = results.Select(r => new EMS.Models.ViewModels.StudentResultViewModel
                {
                    CourseCode = r.Exam.Course.CourseCode,
                    CourseTitle = r.Exam.Course.Title,
                    ExamTitle = r.Exam.Title,
                    ExamDate = r.Exam.ExamDate,
                    TotalMarks = r.Exam.TotalMarks,
                    MarksObtained = r.MarksObtained,
                    SemesterName = r.Exam.Course.Semester.Name // সেমিস্টার নাম সেট করা হলো
                })
                .OrderBy(r => r.SemesterName) // সেমিস্টার অনুযায়ী সাজানো
                .ThenBy(r => r.CourseCode)
                .ToList();

                ViewBag.ExamResults = resultList;

                // ২. সব অ্যাটেনডেন্স আনো (সেমিস্টার সহ)
                var attendanceData = await _context.StudentAttendances
                    .Include(a => a.Course)
                        .ThenInclude(c => c.Semester)
                    .Where(a => a.StudentId == id)
                    .ToListAsync();

                var attendanceList = attendanceData
                    .GroupBy(a => new { a.Course.CourseCode, a.Course.Title, Semester = a.Course.Semester.Name })
                    .Select(g => new EMS.Models.ViewModels.StudentAttendanceViewModel
                    {
                        CourseCode = g.Key.CourseCode,
                        CourseTitle = g.Key.Title,
                        SemesterName = g.Key.Semester, // সেমিস্টার নাম
                        TotalClasses = g.Count(),
                        Present = g.Count(x => x.Status == AttendanceStatus.Present),
                        Late = g.Count(x => x.Status == AttendanceStatus.Late),
                        Absent = g.Count(x => x.Status == AttendanceStatus.Absent)
                    })
                    .OrderBy(x => x.SemesterName)
                    .ToList();

                ViewBag.AttendanceStats = attendanceList;
            }
            // ---------------------------------------

            ViewData["Source"] = source;
            return View(user);
        }

        //User Delete with confirmation. It's for users managment.
        // GET: Admin/Delete/5
        public async Task<IActionResult> Delete(string id, string source)
        {
            if (id == null)
            {
                return NotFound();
            }

            // --- সুরক্ষা: নিজের অ্যাকাউন্ট ডিলিট করা যাবে না ---
            if (id == _userManager.GetUserId(User))
            {
                TempData["StatusMessage"] = "Error: You cannot delete your own account!";
                return RedirectToAction(nameof(ListUsers));
            }

            // ডিলিট করার আগে ইউজারের সব তথ্য দেখিয়ে কনফার্ম করতে হবে
            var user = await _context.Users
                .Include(u => u.StudentProfile)
                    .ThenInclude(s => s.Department)
                .Include(u => u.StudentProfile)
                    .ThenInclude(s => s.Semester)
                .Include(u => u.TeacherProfile)
                    .ThenInclude(t => t.Department)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            ViewData["Source"] = source; // যেখানে থেকে ডিলিট পেজে এসেছি
            return View(user);
        }

        // POST: Admin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, string source)
        {
            // নিজের অ্যাকাউন্ট ডিলিট চেক
            if (id == _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(ListUsers));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // --- রিলেটেড ডেটা ক্লিনআপ (Manual Cleanup) ---

                // যদি স্টুডেন্ট হয়: তার সব একাডেমিক রেকর্ড মুছতে হবে
                var studentProfile = await _context.StudentProfiles.FindAsync(id);
                if (studentProfile != null)
                {
                    // Exam Results মুছতে হবে
                    var results = await _context.ExamResults.Where(r => r.StudentId == id).ToListAsync();
                    _context.ExamResults.RemoveRange(results);

                    // Assignment Submissions মুছতে হবে
                    var submissions = await _context.AssignmentSubmissions.Where(s => s.StudentId == id).ToListAsync();
                    _context.AssignmentSubmissions.RemoveRange(submissions);

                    // Attendance Records মুছতে হবে
                    var attendance = await _context.StudentAttendances.Where(a => a.StudentId == id).ToListAsync();
                    _context.StudentAttendances.RemoveRange(attendance);

                    // সবশেষে প্রোফাইল মুছতে হবে
                    _context.StudentProfiles.Remove(studentProfile);
                }

                // খ. যদি টিচার হয়: তার প্রফেশনাল রেকর্ড ক্লিন করতে হবে
                var teacherProfile = await _context.TeacherProfiles.FindAsync(id);
                if (teacherProfile != null)
                {
                    // টিচারের কোর্সগুলো থেকে তাকে সরিয়ে দেওয়া (Unassign)
                    var courses = await _context.Courses.Where(c => c.TeacherId == id).ToListAsync();
                    foreach (var course in courses)
                    {
                        course.TeacherId = null; // কোর্স থাকবে, কিন্তু টিচার থাকবে না
                    }

                    // প্রোফাইল মুছতে হবে
                    _context.TeacherProfiles.Remove(teacherProfile);
                }

                // ডাটাবেসে এই পরিবর্তনগুলো সেভ
                await _context.SaveChangesAsync();

                // -----------------------------------------------

                // এখন নিরাপদে মেইন ইউজার ডিলিট 
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User and all related data deleted successfully!";

                    if (source == "Students") return RedirectToAction(nameof(Students));
                    if (source == "Teachers") return RedirectToAction(nameof(Teachers));
                    return RedirectToAction(nameof(ListUsers));
                }

                // যদি ডিলিট না হয়
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            // ইউজার না পাওয়া গেলে বা এরর হলে
            return RedirectToAction(nameof(ListUsers));
        }

        //// POST: Admin/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(string id, string source)
        //{
        //    if (id == _userManager.GetUserId(User))
        //    {
        //        return RedirectToAction(nameof(ListUsers));
        //    }

        //    var user = await _userManager.FindByIdAsync(id);
        //    if (user != null)
        //    {
        //        // UserManager ব্যবহার করে ডিলিট করলে এটি অটোমেটিকলি রোল এবং প্রোফাইল ডিলিট করবে (যদি Cascade সেট করা থাকে)
        //        var result = await _userManager.DeleteAsync(user);

        //        if (result.Succeeded)
        //        {
        //            TempData["SuccessMessage"] = "User deleted successfully!";
        //            if (source == "Students") return RedirectToAction(nameof(Students));
        //            if (source == "Teachers") return RedirectToAction(nameof(Teachers));
        //            return RedirectToAction(nameof(ListUsers));
        //        }

        //        // যদি কোনো কারণে ডিলিট না হয়
        //        foreach (var error in result.Errors)
        //        {
        //            ModelState.AddModelError("", error.Description);
        //        }
        //    }
        //    return RedirectToAction(nameof(ListUsers));
        //}


        //User edit option added for user managment.
        // GET: Admin/Edit/5
        public async Task<IActionResult> Edit(string id, string source)
        {
            if (id == null) return NotFound();

            // ইউজার এবং তার প্রোফাইল লোড
            var user = await _context.Users
                .Include(u => u.StudentProfile)
                .Include(u => u.TeacherProfile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            // ইউজারের রোল বের
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            // ViewModel-এ ডেটা লোড
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = userRole,

                // ড্রপডাউন লোড
                DepartmentList = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name"),
                SemesterList = new SelectList(await _context.Semesters.ToListAsync(), "Id", "Name")
            };

            // প্রোফাইল থেকে ডেটা নিয়ে ViewModel
            if (userRole == "Student" && user.StudentProfile != null)
            {
                model.DepartmentId = user.StudentProfile.DepartmentId;
                model.SemesterId = user.StudentProfile.SemesterId;
                model.StudentRoll = user.StudentProfile.StudentRoll;
                model.RegistrationNo = user.StudentProfile.RegistrationNo;
                model.Session = user.StudentProfile.Session;
                model.DateOfBirth = user.StudentProfile.DateOfBirth;
                model.BloodGroup = user.StudentProfile.BloodGroup;
                model.FatherName = user.StudentProfile.FatherName;
                model.MotherName = user.StudentProfile.MotherName;
                model.Address = user.StudentProfile.Address;
            }
            else if (userRole == "Teacher" && user.TeacherProfile != null)
            {
                model.DepartmentId = user.TeacherProfile.DepartmentId;
                model.Designation = user.TeacherProfile.Designation;
            }

            ViewData["Source"] = source; // যেখানে থেকে এডিট পেজে এসেছি
            return View(model);
        }

        // POST: Admin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model, string source)
        {
            if (id != model.Id) return NotFound();

            // ভ্যালিডেশন (পাসওয়ার্ড বাদে)
            if (model.Role == "Student")
            {
                if (model.SemesterId == null) ModelState.AddModelError("SemesterId", "Required for Student");
            }

            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .Include(u => u.StudentProfile)
                    .Include(u => u.TeacherProfile)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null) return NotFound();

                // বেসিক ইনফো আপডেট
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;

                // প্রোফাইল আপডেট
                if (model.Role == "Student")
                {
                    var student = user.StudentProfile;
                    if (student == null)
                    {
                        // যদি প্রোফাইল না থাকে নতুন বানাও
                        student = new StudentProfile { Id = user.Id };
                        _context.StudentProfiles.Add(student);
                    }

                    student.DepartmentId = model.DepartmentId;
                    student.SemesterId = model.SemesterId.Value;
                    student.StudentRoll = model.StudentRoll;
                    student.RegistrationNo = model.RegistrationNo;
                    student.Session = model.Session;
                    student.DateOfBirth = model.DateOfBirth ?? DateTime.Now;
                    student.BloodGroup = model.BloodGroup;
                    student.FatherName = model.FatherName;
                    student.MotherName = model.MotherName;
                    student.Address = model.Address;
                }
                else if (model.Role == "Teacher")
                {
                    var teacher = user.TeacherProfile;
                    if (teacher == null)
                    {
                        teacher = new TeacherProfile { Id = user.Id };
                        _context.TeacherProfiles.Add(teacher);
                    }

                    teacher.DepartmentId = model.DepartmentId;
                    teacher.Designation = model.Designation;
                }

                // সেভ
                await _userManager.UpdateAsync(user); // ইউজার টেবিল আপডেট
                await _context.SaveChangesAsync(); // প্রোফাইল টেবিল আপডেট

                TempData["SuccessMessage"] = "User updated successfully!";
                if (source == "Students") return RedirectToAction(nameof(Students));
                if (source == "Teachers") return RedirectToAction(nameof(Teachers));
                return RedirectToAction(nameof(ListUsers));
            }

            // Error হলে ড্রপডাউন
            model.DepartmentList = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", model.DepartmentId);
            model.SemesterList = new SelectList(await _context.Semesters.ToListAsync(), "Id", "Name", model.SemesterId);
            ViewData["Source"] = source;
            return View(model);
        }


        // GET: Admin/ListUsers
        public async Task<IActionResult> ListUsers(string roleFilter)
        {
            var users = await _userManager.Users.ToListAsync();
            var userListModel = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // যদি রোল ফিল্টার থাকে এবং ইউজারের রোলের সাথে না মিলে
                if (!string.IsNullOrEmpty(roleFilter) && !roles.Contains(roleFilter))
                {
                    continue;
                }

                userListModel.Add(new UserListViewModel
                {
                    UserId = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? "N/A",
                    Roles = string.Join(", ", roles)
                });
            }

            // ড্রপডাউনের জন্য রোল লিস্ট
            ViewBag.RoleList = new SelectList(new List<string> { "Admin", "Teacher", "Student" }, roleFilter);

            return View(userListModel);
        }


        //Admin dashboard all student list showing with filtering system.
        // GET: Admin/Students
        public async Task<IActionResult> Students(string searchString, int? departmentId, int? semesterId, string session)
        {
            // বেসিক কুয়েরি তৈরি
            var studentsQuery = _context.Users
                .Include(u => u.StudentProfile)
                    .ThenInclude(sp => sp.Department)
                .Include(u => u.StudentProfile)
                    .ThenInclude(sp => sp.Semester)
                .Where(u => u.StudentProfile != null) // শুধুমাত্র স্টুডেন্ট
                .AsQueryable();

            // সার্চ লজিক (নাম অথবা রোল দিয়ে সার্চ)
            if (!string.IsNullOrEmpty(searchString))
            {
                studentsQuery = studentsQuery.Where(s =>
                    s.FirstName.Contains(searchString) ||
                    s.LastName.Contains(searchString) ||
                    s.StudentProfile.StudentRoll.Contains(searchString));
            }

            // ডিপার্টমেন্ট ফিল্টার
            if (departmentId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.StudentProfile.DepartmentId == departmentId);
            }

            // সেমিস্টার ফিল্টার
            if (semesterId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.StudentProfile.SemesterId == semesterId);
            }

            // সেশন ফিল্টার
            if (!string.IsNullOrEmpty(session))
            {
                studentsQuery = studentsQuery.Where(s => s.StudentProfile.Session == session);
            }

            // ড্রপডাউনগুলোর জন্য ডেটা প্রস্তুত
            // সেশন লিস্ট (ডেটাবেস থেকে ইউনিক সেশনগুলো বের করা)
            var sessions = await _context.StudentProfiles
                                         .Select(sp => sp.Session)
                                         .Distinct()
                                         .ToListAsync();

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", departmentId);
            ViewData["SemesterId"] = new SelectList(_context.Semesters, "Id", "Name", semesterId);
            ViewData["Session"] = new SelectList(sessions, session);

            // সার্চ ভ্যালু মনে রাখার জন্য
            ViewData["CurrentFilter"] = searchString;

            // ফাইনাল রেজাল্ট
            var students = await studentsQuery.ToListAsync();
            return View(students);
        }

        // Teachers Index page information pass & filtering logic
        // GET: Admin/Teachers
        public async Task<IActionResult> Teachers(string searchString, int? departmentId)
        {
            // বেসিক কুয়েরি
            var teachersQuery = _context.Users
                .Include(u => u.TeacherProfile)
                    .ThenInclude(tp => tp.Department) // ডিপার্টমেন্ট নাম দেখানোর জন্য
                .Where(u => u.TeacherProfile != null) // শুধু টিচারদের
                .AsQueryable();

            // সার্চ লজিক (নাম অথবা ইমেইল)
            if (!string.IsNullOrEmpty(searchString))
            {
                teachersQuery = teachersQuery.Where(t =>
                    t.FirstName.Contains(searchString) ||
                    t.LastName.Contains(searchString) ||
                    t.Email.Contains(searchString));
            }

            // ডিপার্টমেন্ট ফিল্টার
            if (departmentId.HasValue)
            {
                teachersQuery = teachersQuery.Where(t => t.TeacherProfile.DepartmentId == departmentId);
            }

            // ড্রপডাউন এবং ভিউ ডেটা সেট করা
            ViewBag.DepartmentId = new SelectList(_context.Departments, "Id", "Name", departmentId);
            ViewData["CurrentFilter"] = searchString;

            return View(await teachersQuery.ToListAsync());
        }


        // Admin এর জন্য অ্যাটেনডেন্স ম্যানেজমেন্ট
        // GET: Admin/ManageAttendance
        public async Task<IActionResult> ManageAttendance(int? courseId, DateTime? date)
        {
            var attendanceDate = date ?? DateTime.Now;

            // সব কোর্স লোড(অ্যাডমিনের জন্য সব ওপেন)
            var courses = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .OrderBy(c => c.Department.Name)
                .ThenBy(c => c.Semester.Name)
                .ToListAsync();

            // ড্রপডাউন পপুলেট করা
            ViewBag.CourseList = new SelectList(courses.Select(c => new {
                Id = c.Id,
                Title = $"{c.CourseCode} - {c.Title} ({c.Department?.Name})"
            }), "Id", "Title", courseId);

            if (courseId == null)
            {
                return View(new AttendanceViewModel { Date = attendanceDate });
            }

            // ২. স্টুডেন্ট এবং অ্যাটেনডেন্স লোড করা
            var selectedCourse = courses.FirstOrDefault(c => c.Id == courseId);
            var students = await _context.Users
                .Include(u => u.StudentProfile)
                .Where(u => u.StudentProfile != null &&
                            u.StudentProfile.DepartmentId == selectedCourse.DepartmentId &&
                            u.StudentProfile.SemesterId == selectedCourse.SemesterId)
                .OrderBy(u => u.StudentProfile.StudentRoll)
                .ToListAsync();

            var existingAttendance = await _context.StudentAttendances
                .Where(a => a.CourseId == courseId && a.Date.Date == attendanceDate.Date)
                .ToListAsync();

            var model = new AttendanceViewModel
            {
                CourseId = selectedCourse.Id,
                CourseTitle = selectedCourse.Title,
                CourseCode = selectedCourse.CourseCode,
                Date = attendanceDate,
                Students = students.Select(s => {
                    var record = existingAttendance.FirstOrDefault(a => a.StudentId == s.Id);
                    return new StudentAttendanceRow
                    {
                        StudentId = s.Id,
                        StudentName = $"{s.FirstName} {s.LastName}",
                        RollNo = s.StudentProfile.StudentRoll,
                        Status = record != null ? record.Status : AttendanceStatus.Present
                    };
                }).ToList()
            };

            return View(model);
        }

        // POST: Admin/ManageAttendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageAttendance(AttendanceViewModel model)
        {
            // আগের রেকর্ড মুছে নতুন করে সেভ করা
            var existingRecords = await _context.StudentAttendances
                .Where(a => a.CourseId == model.CourseId && a.Date.Date == model.Date.Date)
                .ToListAsync();

            if (existingRecords.Any()) _context.StudentAttendances.RemoveRange(existingRecords);

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
            TempData["SuccessMessage"] = "Attendance updated successfully!";
            return RedirectToAction("ManageAttendance", new { courseId = model.CourseId, date = model.Date });
        }

        // Admin এর জন্য এক্সাম ম্যানেজমেন্ট 
        // GET: Admin/ManageExams
        public async Task<IActionResult> ManageExams(string searchString, int? courseId, ExamType? examType)
        {
            // এক্সাম ডেটা লোড করো (কোর্স, ডিপার্টমেন্ট, সেমিস্টার, টিচার সহ)
            var examsQuery = _context.Exams
                .Include(e => e.Course)
                    .ThenInclude(c => c.Department)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Semester)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Teacher) // কে পরীক্ষা নিয়েছে দেখা
                .AsQueryable();

            // ফিল্টার: নাম দিয়ে সার্চ
            if (!string.IsNullOrEmpty(searchString))
            {
                examsQuery = examsQuery.Where(e => e.Title.Contains(searchString) || e.Course.CourseCode.Contains(searchString));
            }

            // ফিল্টার: কোর্স অনুযায়ী
            if (courseId.HasValue)
            {
                examsQuery = examsQuery.Where(e => e.CourseId == courseId);
            }

            // ফিল্টার: এক্সামের ধরন (Midterm/Final etc)
            if (examType.HasValue)
            {
                examsQuery = examsQuery.Where(e => e.ExamType == examType);
            }

            // ড্রপডাউন ডেটা
            ViewBag.CourseList = new SelectList(_context.Courses, "Id", "CourseCode", courseId);
            ViewData["CurrentFilter"] = searchString;

            // লেটেস্ট এক্সাম আগে দেখাবে
            var exams = await examsQuery.OrderByDescending(e => e.ExamDate).ToListAsync();
            return View(exams);
        }

        // GET: Admin/ExamResults/5
        public async Task<IActionResult> ExamResults(int? id)
        {
            if (id == null) return NotFound();

            // এক্সাম ইনফো
            var exam = await _context.Exams
                .Include(e => e.Course)
                    .ThenInclude(c => c.Department)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Semester)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null) return NotFound();

            // রেজাল্ট এবং স্টুডেন্ট ইনফো লোড করো
            var results = await _context.ExamResults
                .Include(r => r.Student)
                    .ThenInclude(s => s.StudentProfile)
                .Where(r => r.ExamId == id)
                .OrderBy(r => r.Student.StudentProfile.StudentRoll)
                .ToListAsync();

            ViewBag.Exam = exam;

            return View(results);
        }

        // GET: Admin/ManageAssignments
        public async Task<IActionResult> ManageAssignments(string searchString, int? courseId)
        {
            // সব অ্যাসাইনমেন্ট লোড (টিচার, কোর্স, সেমিস্টার সহ)
            var assignmentsQuery = _context.Assignments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Department)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Semester)
                .Include(a => a.Teacher) // কে অ্যাসাইনমেন্ট দিয়েছে
                .AsQueryable();

            // সার্চ ফিল্টার
            if (!string.IsNullOrEmpty(searchString))
            {
                assignmentsQuery = assignmentsQuery.Where(a => a.Title.Contains(searchString) || a.Course.CourseCode.Contains(searchString));
            }

            // কোর্স ফিল্টার
            if (courseId.HasValue)
            {
                assignmentsQuery = assignmentsQuery.Where(a => a.CourseId == courseId);
            }

            // ড্রপডাউন ডেটা
            ViewBag.CourseList = new SelectList(_context.Courses, "Id", "CourseCode", courseId);
            ViewData["CurrentFilter"] = searchString;

            // লেটেস্ট অ্যাসাইনমেন্ট আগে দেখাবে
            var assignments = await assignmentsQuery.OrderByDescending(a => a.CreatedDate).ToListAsync();
            return View(assignments);
        }

        // GET: Admin/AssignmentSubmissions/5
        public async Task<IActionResult> AssignmentSubmissions(int? id)
        {
            if (id == null) return NotFound();

            // অ্যাসাইনমেন্ট ডিটেইলস
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Department)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Semester)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return NotFound();

            // সাবমিশন লিস্ট লোড
            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.Student)
                    .ThenInclude(u => u.StudentProfile)
                .Where(s => s.AssignmentId == id)
                .OrderBy(s => s.Student.StudentProfile.StudentRoll)
                .ToListAsync();

            ViewBag.Assignment = assignment;
            return View(submissions);
        }


        // GET: Admin/PromoteStudents
        public async Task<IActionResult> PromoteStudents(int? departmentId, int? currentSemesterId)
        {
            var departments = await _context.Departments.ToListAsync();
            var semesters = await _context.Semesters.ToListAsync();

            var model = new PromoteStudentViewModel
            {
                DepartmentList = new SelectList(departments, "Id", "Name", departmentId),
                SemesterList = new SelectList(semesters, "Id", "Name", currentSemesterId),
                DepartmentId = departmentId ?? 0,
                CurrentSemesterId = currentSemesterId ?? 0
            };

            if (departmentId.HasValue && currentSemesterId.HasValue)
            {
                var students = await _context.Users
                    .Include(u => u.StudentProfile)
                    .Where(u => u.StudentProfile != null &&
                                u.StudentProfile.DepartmentId == departmentId &&
                                u.StudentProfile.SemesterId == currentSemesterId)
                    .OrderBy(u => u.StudentProfile.StudentRoll)
                    .ToListAsync();

                // ১. এই সেমিস্টার ও ডিপার্টমেন্টের সব এক্সাম লোড করো
                var currentExams = await _context.Exams
                    .Include(e => e.Course)
                    .Where(e => e.Course.DepartmentId == departmentId && e.Course.SemesterId == currentSemesterId)
                    .ToListAsync();

                var examIds = currentExams.Select(e => e.Id).ToList();

                // ২. এই এক্সামগুলোর সব রেজাল্ট লোড করো
                var results = await _context.ExamResults
                    .Where(r => examIds.Contains(r.ExamId))
                    .ToListAsync();

                // ৩. প্রতিটি স্টুডেন্টের পাস/ফেইল চেক করো
                model.Students = students.Select(s => {

                    var studentResults = results.Where(r => r.StudentId == s.Id).ToList();
                    int failCount = 0;

                    // লজিক: ৪০% এর কম পেলে ফেইল
                    foreach (var result in studentResults)
                    {
                        var exam = currentExams.FirstOrDefault(e => e.Id == result.ExamId);
                        if (exam != null && result.MarksObtained < (exam.TotalMarks * 0.40))
                        {
                            failCount++;
                        }
                    }

                    return new StudentPromoteItem
                    {
                        StudentId = s.Id,
                        Name = $"{s.FirstName} {s.LastName}",
                        RollNo = s.StudentProfile.StudentRoll,
                        FailedCount = failCount,

                        // যদি ১টাও ফেইল থাকে, তবে মেসেজ দেখাও
                        StatusMessage = failCount > 0 ? $"Failed in {failCount} Exam(s)" : "All Clear",

                        // যদি ফেইল না করে থাকে, তবেই অটোমেটিক সিলেক্ট হবে
                        IsSelected = (failCount == 0)
                    };
                }).ToList();
            }

            return View(model);
        }

        // POST: Admin/PromoteStudents
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteStudents(PromoteStudentViewModel model)
        {
            // ১. ভ্যালিডেশন
            if (model.CurrentSemesterId == model.NextSemesterId)
            {
                ModelState.AddModelError("", "Current and Next semester cannot be the same.");
            }

            // ২. সিলেক্টেড স্টুডেন্টদের খুঁজে বের করে আপডেট করা
            var selectedStudentIds = model.Students.Where(s => s.IsSelected).Select(s => s.StudentId).ToList();

            if (selectedStudentIds.Any())
            {
                var studentsToPromote = await _context.StudentProfiles
                    .Where(sp => selectedStudentIds.Contains(sp.Id))
                    .ToListAsync();

                foreach (var profile in studentsToPromote)
                {
                    profile.SemesterId = model.NextSemesterId; // সেমিস্টার আপডেট
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Successfully promoted {selectedStudentIds.Count} students!";

                return RedirectToAction(nameof(Students));
            }
            else
            {
                ModelState.AddModelError("", "No students selected for promotion.");
            }

            // এরর হলে পেজ রিলোড
            var departments = await _context.Departments.ToListAsync();
            var semesters = await _context.Semesters.ToListAsync();
            model.DepartmentList = new SelectList(departments, "Id", "Name", model.DepartmentId);
            model.SemesterList = new SelectList(semesters, "Id", "Name", model.CurrentSemesterId);

            return View(model);
        }


        //Admin Dashboard Data passing for chart and information
        // GET: Admin/GetDashboardStats (AJAX Call এর জন্য)
        [HttpGet]
        public async Task<JsonResult> GetDashboardStats()
        {
            var today = DateTime.Now.Date;

            // Basic data count
            var totalStudents = await _context.StudentProfiles.CountAsync();
            var totalTeachers = await _context.TeacherProfiles.CountAsync();
            var totalCourses = await _context.Courses.CountAsync();
            var totalNotices = await _context.Notices.CountAsync();
            var totalDepartments = await _context.Departments.CountAsync();
            var totalExams = await _context.Exams.CountAsync();

            var presentToday = await _context.StudentAttendances
                .Where(a => a.Date.Date == today && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late))
                .Select(a => a.StudentId)
                .Distinct()
                .CountAsync();

            //  Department wise Students
            var studentsByDept = await _context.StudentProfiles
                .Include(s => s.Department)
                .GroupBy(s => s.Department.Name)
                .Select(g => new { label = g.Key, value = g.Count() })
                .ToListAsync();

            //  Session wise Students
            var studentsBySession = await _context.StudentProfiles
                .GroupBy(s => s.Session)
                .Select(g => new { label = g.Key, value = g.Count() })
                .OrderBy(x => x.label)
                .ToListAsync();

            //Today's Attendance Summary
            // আজকের মোট Present, Absent, Late সংখ্যা
            var attendanceStats = await _context.StudentAttendances
                .Where(a => a.Date.Date == today)
                .GroupBy(a => a.Status)
                .Select(g => new { status = g.Key.ToString(), count = g.Count() })
                .ToListAsync();

            // নোটিশ লিস্ট
            var recentNotices = await _context.Notices
                .OrderByDescending(n => n.PostedDate)
                .Take(5)
                .Select(n => new {
                    title = n.Title,
                    date = n.PostedDate.ToString("dd MMM"),
                    isForStudent = n.IsForStudents,
                    isForTeacher = n.IsForTeachers
                })
                .ToListAsync();

            return Json(new
            {
                totalStudents,
                totalTeachers,
                totalCourses,
                totalNotices,
                totalDepartments,
                totalExams,
                presentToday,
                studentsByDept,
                studentsBySession, 
                attendanceStats,   
                recentNotices      
            });
        }

    }
}