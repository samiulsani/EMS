using System.ComponentModel.DataAnnotations; // এটা যোগ করো

namespace EMS.Models
{
    public class Course
    {
        public int Id { get; set; } // Primary Key

        [Required]
        [StringLength(50)]
        public string CourseCode { get; set; } // যেমন: "CSE-101"

        [Required]
        [StringLength(255)]
        public string Title { get; set; } // যেমন: "Introduction to Programming"

        [Required]
        public int Credits { get; set; }

        // Foreign Key: কোর্সটি কোন ডিপার্টমেন্টের?
        public int DepartmentId { get; set; }

        // Navigation Property: ডিপার্টমেন্ট টেবিলের সাথে লিঙ্ক
        public Department? Department { get; set; }

        // সেমিস্টার (কোর্সটি কোন সেমিস্টারের)
        [Required]
        public int SemesterId { get; set; }
        public Semester? Semester { get; set; }
    }
}