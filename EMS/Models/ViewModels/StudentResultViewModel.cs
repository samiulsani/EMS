namespace EMS.Models.ViewModels
{
    public class StudentResultViewModel
    {
        public string CourseCode { get; set; }
        public string CourseTitle { get; set; }
        public string ExamTitle { get; set; } // যেমন: Midterm
        public DateTime ExamDate { get; set; }
        public double TotalMarks { get; set; }
        public double MarksObtained { get; set; }

        // --- গ্রেড ক্যালকুলেশন লজিক (Read-Only Property) ---
        public string Grade
        {
            get
            {
                double percentage = (MarksObtained / TotalMarks) * 100;

                if (percentage >= 80) return "A+";
                if (percentage >= 75) return "A";
                if (percentage >= 70) return "A-";
                if (percentage >= 65) return "B+";
                if (percentage >= 60) return "B";
                if (percentage >= 55) return "B-";
                if (percentage >= 50) return "C+";
                if (percentage >= 45) return "C";
                if (percentage >= 40) return "D";
                return "F";
            }
        }

        public string GradeColor
        {
            get
            {
                if (Grade == "F") return "danger"; // লাল
                if (Grade.Contains("A")) return "success"; // সবুজ
                return "primary"; // নীল (B, C, D এর জন্য)
            }
        }
    }
}