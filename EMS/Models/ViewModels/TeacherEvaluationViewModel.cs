using System.ComponentModel.DataAnnotations;

namespace EMS.Models.ViewModels
{
    public class TeacherEvaluationViewModel
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseTitle { get; set; }

        public string TeacherId { get; set; }
        public string TeacherName { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Please write a short feedback.")]
        [StringLength(1000)]
        public string Comment { get; set; }

        public bool IsRated { get; set; } // ছাত্রটি কি ইতিমধ্যে রিভিউ দিয়েছে?
    }
}