
using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }

        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        /// 0 = Student, 1 = Professor, 2 = Admin
        [Range(0, 2)]
        public byte Role { get; set; }

        /// <summary>Convenience: "Student" | "Professor" | "Admin"</summary>
        public string RoleName { get; set; } = string.Empty;

        /// <summary>Required for students; null for admins; optional for professors.</summary>
        public int? GroupId { get; set; }

        public bool IsFirstLogin { get; set; }
    }
}
