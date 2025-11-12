using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Models
{
    public class ExamResult
    {
        public int Id { get; set; }

        [Required]
        [Range(0, 200)]
        public double MarksObtained { get; set; } // প্রাপ্ত নম্বর

        // --- সম্পর্ক ---
        [Required]
        public int ExamId { get; set; }
        public Exam? Exam { get; set; }

        [Required]
        public string StudentId { get; set; }
        [ForeignKey("StudentId")]
        public ApplicationUser? Student { get; set; }
    }
}