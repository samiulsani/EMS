using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EMS.Models.ViewModels.ClassSchedule
{
    public class ClassRoutineViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Select Course")]
        public int CourseId { get; set; }
        public SelectList? CourseList { get; set; } // ড্রপডাউনের জন্য

        [Required]
        [Display(Name = "Day")]
        public DayOfWeekEnum Day { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Required]
        [Display(Name = "Room Number")]
        public string RoomNo { get; set; }
    }
}