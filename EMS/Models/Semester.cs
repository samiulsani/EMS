using System.ComponentModel.DataAnnotations;

namespace EMS.Models
{
    public class Semester
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } // যেমন: "1st Semester", "2nd Semester"

        // Navigation Properties (Optional but good practice)
        // এই সেমিস্টারে কোন কোন কোর্স আছে
        public ICollection<Course> Courses { get; set; } = new List<Course>();

        // এই সেমিস্টারে কোন কোন স্টুডেন্ট আছে
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}