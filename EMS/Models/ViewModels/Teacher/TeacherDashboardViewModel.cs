using System.ComponentModel.DataAnnotations;
using EMS.Models; // Course মডেল ব্যবহারের জন্য
using System.Collections.Generic; // List ব্যবহারের জন্য

namespace EMS.Models.ViewModels.Teacher // <--- লক্ষ্য করো: নতুন ফোল্ডার অনুযায়ী Namespace
{
    public class TeacherDashboardViewModel
    {
        public string Name { get; set; }
        public string Designation { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        // ভবিষ্যতে আমরা এখানে "Assigned Courses" এর লিস্ট দেখাবো
        public int TotalAssignedCourses { get; set; } = 0;
        public List<Course> AssignedCourses { get; set; } = new List<Course>();
    }
}