using EMS.Data;
using EMS.Models;
using EMS.Models.ViewModels;
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
            _context = context; // <--- context ইনিশিয়ালাইজ করো
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

        // --- CreateUser (GET) মেথডটি রিপ্লেস করো ---
        public async Task<IActionResult> CreateUser()
        {
            // ড্রপডাউনের জন্য সব ডেটা লোড করো
            var roles = await _roleManager.Roles
                .Where(r => r.Name == "Student" || r.Name == "Teacher") // শুধু স্টুডেন্ট বা টিচার রোল দেখাও
                .ToListAsync();

            var departments = await _context.Departments.ToListAsync();
            var semesters = await _context.Semesters.ToListAsync();

            var model = new CreateUserViewModel
            {
                RoleList = new SelectList(roles, "Name", "Name"),
                DepartmentList = new SelectList(departments, "Id", "Name"),
                SemesterList = new SelectList(semesters, "Id", "Name")
            };
            return View(model);
        }

        // --- CreateUser (POST) মেথডটি রিপ্লেস করো ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
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
                // ধাপ ১: ApplicationUser (লগইন) তৈরি করো
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
                    // ধাপ ২: ইউজারকে রোলে অ্যাসাইন করো
                    await _userManager.AddToRoleAsync(user, model.Role);

                    // ধাপ ৩: রোল অনুযায়ী প্রোফাইল তৈরি করো
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

                    await _context.SaveChangesAsync(); // প্রোফাইল সেভ করো

                    TempData["SuccessMessage"] = "User created successfully!";
                    return RedirectToAction("ListUsers"); // সফল হলে ইউজার লিস্টে পাঠাও
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // --- Error হলে ড্রপডাউনগুলো আবার লোড করো ---
            var roles = await _roleManager.Roles
                .Where(r => r.Name == "Student" || r.Name == "Teacher")
                .ToListAsync();
            var departments = await _context.Departments.ToListAsync();
            var semesters = await _context.Semesters.ToListAsync();

            model.RoleList = new SelectList(roles, "Name", "Name", model.Role);
            model.DepartmentList = new SelectList(departments, "Id", "Name", model.DepartmentId);
            model.SemesterList = new SelectList(semesters, "Id", "Name", model.SemesterId);

            return View(model);
        }

        //Get Details from user. It's for user managment 
        // GET: Admin/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // ইউজারকে খুঁজো এবং সাথে তার প্রোফাইল, ডিপার্টমেন্ট ও সেমিস্টার তথ্যও নিয়ে এসো
            var user = await _context.Users
                .Include(u => u.StudentProfile)
                    .ThenInclude(s => s.Department) // স্টুডেন্টের ডিপার্টমেন্ট
                .Include(u => u.StudentProfile)
                    .ThenInclude(s => s.Semester)   // স্টুডেন্টের সেমিস্টার
                .Include(u => u.TeacherProfile)
                    .ThenInclude(t => t.Department) // টিচারের ডিপার্টমেন্ট
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        //User Delete with confirmation. It's for users managment.
        // GET: Admin/Delete/5
        public async Task<IActionResult> Delete(string id)
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

            return View(user);
        }

        // POST: Admin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (id == _userManager.GetUserId(User))
            {
                return RedirectToAction(nameof(ListUsers));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // UserManager ব্যবহার করে ডিলিট করলে এটি অটোমেটিকলি রোল এবং প্রোফাইল ডিলিট করবে (যদি Cascade সেট করা থাকে)
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User deleted successfully!";
                    return RedirectToAction(nameof(ListUsers));
                }

                // যদি কোনো কারণে ডিলিট না হয়
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return RedirectToAction(nameof(ListUsers));
        }


        //User edit option added for user managment.
        // GET: Admin/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            // ইউজার এবং তার প্রোফাইল লোড করো
            var user = await _context.Users
                .Include(u => u.StudentProfile)
                .Include(u => u.TeacherProfile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            // ইউজারের রোল বের করো
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            // ViewModel-এ ডেটা লোড করো
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = userRole,

                // ড্রপডাউন লোড করো
                DepartmentList = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name"),
                SemesterList = new SelectList(await _context.Semesters.ToListAsync(), "Id", "Name")
            };

            // প্রোফাইল থেকে ডেটা নিয়ে ViewModel-এ বসাও
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

            return View(model);
        }

        // POST: Admin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (id != model.Id) return NotFound();

            // ভ্যালিডেশন (পাসওয়ার্ড বাদে)
            if (model.Role == "Student")
            {
                if (model.SemesterId == null) ModelState.AddModelError("SemesterId", "Required for Student");
                // ... অন্যান্য প্রয়োজনীয় ফিল্ড চেক করতে পারো ...
            }

            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .Include(u => u.StudentProfile)
                    .Include(u => u.TeacherProfile)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null) return NotFound();

                // ১. বেসিক ইনফো আপডেট
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                // user.Email আপডেট করা সাবধানতার বিষয়, আপাতত বাদ রাখলাম

                // ২. প্রোফাইল আপডেট
                if (model.Role == "Student")
                {
                    var student = user.StudentProfile;
                    if (student == null)
                    {
                        // যদি প্রোফাইল না থাকে (অস্বাভাবিক), নতুন বানাও
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

                // ৩. সেভ করো
                await _userManager.UpdateAsync(user); // ইউজার টেবিল আপডেট
                await _context.SaveChangesAsync(); // প্রোফাইল টেবিল আপডেট

                TempData["SuccessMessage"] = "User updated successfully!";
                return RedirectToAction(nameof(ListUsers));
            }

            // Error হলে ড্রপডাউন আবার পাঠাও
            model.DepartmentList = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", model.DepartmentId);
            model.SemesterList = new SelectList(await _context.Semesters.ToListAsync(), "Id", "Name", model.SemesterId);
            return View(model);
        }


        public async Task<IActionResult> ListUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userListModel = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userListModel.Add(new UserListViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = string.Join(", ", roles)
                });
            }

            return View(userListModel);
        }


        //Admin dashboard all student list showing with filtering system.
        // GET: Admin/Students
        public async Task<IActionResult> Students(string searchString, int? departmentId, int? semesterId, string session)
        {
            // ১. বেসিক কুয়েরি তৈরি (এখনো এক্সিকিউট হয়নি)
            var studentsQuery = _context.Users
                .Include(u => u.StudentProfile)
                    .ThenInclude(sp => sp.Department)
                .Include(u => u.StudentProfile)
                    .ThenInclude(sp => sp.Semester)
                .Where(u => u.StudentProfile != null) // শুধুমাত্র স্টুডেন্টদের নাও
                .AsQueryable();

            // ২. সার্চ লজিক (নাম অথবা রোল দিয়ে সার্চ)
            if (!string.IsNullOrEmpty(searchString))
            {
                studentsQuery = studentsQuery.Where(s =>
                    s.FirstName.Contains(searchString) ||
                    s.LastName.Contains(searchString) ||
                    s.StudentProfile.StudentRoll.Contains(searchString));
            }

            // ৩. ডিপার্টমেন্ট ফিল্টার
            if (departmentId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.StudentProfile.DepartmentId == departmentId);
            }

            // ৪. সেমিস্টার ফিল্টার
            if (semesterId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.StudentProfile.SemesterId == semesterId);
            }

            // ৫. সেশন ফিল্টার
            if (!string.IsNullOrEmpty(session))
            {
                studentsQuery = studentsQuery.Where(s => s.StudentProfile.Session == session);
            }

            // ৬. ড্রপডাউনগুলোর জন্য ডেটা প্রস্তুত করা
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

            // ৭. ফাইনাল রেজাল্ট
            var students = await studentsQuery.ToListAsync();
            return View(students);
        }

        // GET: Admin/Teachers
        public async Task<IActionResult> Teachers()
        {
            // শুধুমাত্র যাদের TeacherProfile আছে (মানে টিচার) তাদের খুঁজে বের করো
            var teachers = await _context.Users
                .Include(u => u.TeacherProfile)
                    .ThenInclude(tp => tp.Department) // ডিপার্টমেন্ট নাম দেখানোর জন্য
                .Where(u => u.TeacherProfile != null) // ফিল্টার
                .ToListAsync();

            return View(teachers);
        }

    }
}