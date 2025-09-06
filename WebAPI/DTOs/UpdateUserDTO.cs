// Path: WebAPI/DTOs/UserUpdateDto.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public sealed class UserUpdateDto : IValidatableObject
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        /// 0 = Student, 1 = Professor, 2 = Admin
        [Range(0, 2)]
        public byte Role { get; set; }

        /// Required for students; null for admins; optional for professors.
        public int? GroupId { get; set; }

        public bool IsFirstLogin { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            if (Role == 0 && GroupId == null)
                yield return new ValidationResult("Student must have GroupId.", new[] { nameof(GroupId) });

            if (Role == 2 && GroupId != null)
                yield return new ValidationResult("Admin must not have GroupId.", new[] { nameof(GroupId) });
        }
    }
}
