using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Models
{
    public class TeacherEvaluation
    {
        public int Id { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; } // 1 (Poor) - 5 (Excellent)

        [Required]
        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Anonymous Feedback")]
        public string Comment { get; set; }

        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        // --- সম্পর্ক (Relationships) ---

        // কোন কোর্সের জন্য?
        [Required]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        // কোন টিচারের জন্য?
        [Required]
        public string TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public ApplicationUser? Teacher { get; set; }

        // কে রিভিউ দিয়েছে? (এটা শুধু ডুপ্লিকেট চেক করার জন্য, ডিসপ্লে করা হবে না)
        [Required]
        public string StudentId { get; set; }
        [ForeignKey("StudentId")]
        public ApplicationUser? Student { get; set; }
    }
}