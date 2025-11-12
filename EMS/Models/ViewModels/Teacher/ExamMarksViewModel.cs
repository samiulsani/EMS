using System.ComponentModel.DataAnnotations;

namespace EMS.Models.ViewModels.Teacher
{
    public class ExamMarksViewModel
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }
        public string CourseTitle { get; set; }
        public double TotalMarks { get; set; }
        public DateTime ExamDate { get; set; }

        public List<StudentMarksRow> Students { get; set; } = new List<StudentMarksRow>();
    }

    public class StudentMarksRow
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string RollNo { get; set; }

        [Range(0, 200)]
        public double MarksObtained { get; set; } // টিচার এখানে মার্কস দেবেন
    }
}