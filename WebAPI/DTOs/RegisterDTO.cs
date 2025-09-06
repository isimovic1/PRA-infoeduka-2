// WebAPI/DTOs/RegisterDto.cs
using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public sealed class RegisterDto : IValidatableObject
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

        //[Required, StringLength(100, MinimumLength = 8)]
        //public string Password { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$",
         ErrorMessage = "Password must contain letters and numbers (min 8 chars).")]
        public string Password { get; set; } = string.Empty;

        // Optional:
        // [Compare("Password")]
        // public string? ConfirmPassword { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            if (Role == 0 && GroupId == null)
                yield return new ValidationResult("Student must have GroupId.", new[] { nameof(GroupId) });

            if (Role == 2 && GroupId != null)
                yield return new ValidationResult("Admin must not have GroupId.", new[] { nameof(GroupId) });
        }
    }
}
