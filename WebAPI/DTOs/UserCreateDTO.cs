// WebAPI/DTOs/UserCreateDto.cs
using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public class UserCreateDto
    {
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        /// 0 = Student, 1 = Professor, 2 = Admin
        [Range(0, 2)]
        public byte Role { get; set; }

        /// Required for students; null for admins; optional for professors.
        public int? GroupId { get; set; }

        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }
}
