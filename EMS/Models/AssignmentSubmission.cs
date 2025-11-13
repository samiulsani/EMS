using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Models
{
    public class AssignmentSubmission
    {
        public int Id { get; set; }

        [Required]
        public string FileUrl { get; set; } // আপলোড করা ফাইলের পাথ

        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        // --- মার্কিং সেকশন ---
        [Range(0, 100)]
        public double? MarksObtained { get; set; } // ফাইনাল মার্কস (যা টিচার দেবেন)

        public string? TeacherFeedback { get; set; }

        // --- AI ইন্টিগ্রেশন ফিল্ড ---
        public double? AIMarks { get; set; }       // AI-এর দেওয়া মার্কস
        public string? AIReview { get; set; }      // AI-এর ফিডব্যাক
        public double? AIPlagiarismScore { get; set; } // AI কপির সম্ভাবনা (%)

        // --- সম্পর্ক ---
        [Required]
        public int AssignmentId { get; set; }
        public Assignment? Assignment { get; set; }

        [Required]
        public string StudentId { get; set; }
        [ForeignKey("StudentId")]
        public ApplicationUser? Student { get; set; }
    }
}