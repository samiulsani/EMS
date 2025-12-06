using System;
using System.Collections.Generic;

namespace EMS.Models.ViewModels
{
    public class TeacherEvaluationReportViewModel
    {
        public string TeacherId { get; set; }
        public string TeacherName { get; set; }
        public string Designation { get; set; }
        public string Department { get; set; }

        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }

        // বিস্তারিত দেখার জন্য কমেন্ট লিস্ট
        public List<EvaluationDetail> Comments { get; set; } = new List<EvaluationDetail>();
    }

    public class EvaluationDetail
    {
        public string CourseCode { get; set; }
        public string Semester { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }
    }
}