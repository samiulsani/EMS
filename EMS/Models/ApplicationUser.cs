using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations; // এটা যোগ করো

namespace EMS.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        // Foreign Key: এই ইউজার কোন ডিপার্টমেন্টের?
        public int? DepartmentId { get; set; }

        // Navigation Property: ডিপার্টমেন্ট টেবিলের সাথে লিঙ্ক
        public Department? Department { get; set; }
    }
}