using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EMS.Models.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; } // ইউজার আইডি মনে রাখার জন্য

        [Display(Name = "Email")]
        public string Email { get; set; } // ইমেইল দেখানো হবে, কিন্তু এডিট করা যাবে না (সাধারণত)

        // --- সাধারণ তথ্য ---
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "User Role")]
        public string Role { get; set; } // রোল দেখানো হবে, কিন্তু এডিট করা জটিল (তাই রিড-অনলি রাখবো)

        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        public SelectList? DepartmentList { get; set; }

        // --- টিচার স্পেসিফিক ---
        [Display(Name = "Designation")]
        public string? Designation { get; set; }

        // --- স্টুডেন্ট স্পেসিফিক ---
        [Display(Name = "Student Roll")]
        public string? StudentRoll { get; set; }

        [Display(Name = "Registration No")]
        public string? RegistrationNo { get; set; }

        public string? Session { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? BloodGroup { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string? Address { get; set; }

        [Display(Name = "Semester")]
        public int? SemesterId { get; set; }
        public SelectList? SemesterList { get; set; }
    }
}