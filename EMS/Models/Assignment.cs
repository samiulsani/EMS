using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } // প্রশ্নের বিবরণ

        [Required]
        public DateTime Deadline { get; set; }

        [Required]
        public double TotalMarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // --- সম্পর্ক ---
        [Required]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        [Required]
        public string TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public ApplicationUser? Teacher { get; set; }
    }
}