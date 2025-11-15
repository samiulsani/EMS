using System.ComponentModel.DataAnnotations;

namespace EMS.Models
{
    public class ClassRoutine
    {
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }
        public Course? Course { get; set; } // কোর্স থেকে আমরা টিচার ও সেমিস্টার তথ্য পাবো

        [Required]
        public DayOfWeekEnum Day { get; set; } // শনিবার, রবিবার...

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; } // ক্লাস শুরুর সময়

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; } // ক্লাস শেষ হওয়ার সময়

        [Required]
        [StringLength(50)]
        public string RoomNo { get; set; } // যেমন: "Room-302" বা "Lab-A"
    }
}