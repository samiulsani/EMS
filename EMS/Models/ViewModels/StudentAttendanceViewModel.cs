namespace EMS.Models.ViewModels
{
    public class StudentAttendanceViewModel
    {
        public string CourseCode { get; set; }
        public string CourseTitle { get; set; }
        public int TotalClasses { get; set; }
        public int Present { get; set; }
        public int Late { get; set; }
        public int Absent { get; set; }

        // --- নতুন প্রপার্টি ---
        public string SemesterName { get; set; }

        // উপস্থিতির হার (Present + Late কে উপস্থিত ধরা হলো)
        public double Percentage => TotalClasses == 0 ? 0 :
            Math.Round(((double)(Present + Late) / TotalClasses) * 100, 2);
    }
}