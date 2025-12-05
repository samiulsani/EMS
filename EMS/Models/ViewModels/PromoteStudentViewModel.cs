using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EMS.Models.ViewModels
{
    public class PromoteStudentViewModel
    {
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        public SelectList? DepartmentList { get; set; }

        [Display(Name = "Current Semester (From)")]
        public int CurrentSemesterId { get; set; }
        public SelectList? SemesterList { get; set; }

        [Display(Name = "Promote To (Next Semester)")]
        public int NextSemesterId { get; set; }

        // চেকবক্সের মাধ্যমে স্টুডেন্ট সিলেক্ট করার জন্য
        public List<StudentPromoteItem> Students { get; set; } = new List<StudentPromoteItem>();
    }

    public class StudentPromoteItem
    {
        public string StudentId { get; set; }
        public string Name { get; set; }
        public string RollNo { get; set; }
        public bool IsSelected { get; set; }

        // --- নতুন প্রপার্টি ---
        public int FailedCount { get; set; } // কয়টি সাবজেক্টে ফেইল
        public string StatusMessage { get; set; } // মেসেজ (All Clear / Failed)
    }
}