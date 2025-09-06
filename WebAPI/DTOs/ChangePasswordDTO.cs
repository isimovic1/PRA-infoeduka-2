
using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public sealed class ChangePasswordDto
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$",
            ErrorMessage = "Password must contain letters and numbers (min 8 chars).")]
        public string NewPassword { get; set; } = string.Empty;
    }
}


//using System.ComponentModel.DataAnnotations;

//namespace WebAPI.DTOs
//{
//    public sealed class ChangePasswordDto
//    {
//        [Required] public string OldPassword { get; set; } = string.Empty;

//        [Required, StringLength(100, MinimumLength = 8)]
//        public string NewPassword { get; set; } = string.Empty;
//    }
//}

