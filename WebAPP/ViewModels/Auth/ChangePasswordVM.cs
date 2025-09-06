using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.Auth
{
    public class ChangePasswordVM
    {
        [Required, DataType(DataType.Password)]
        public string OldPassword { get; set; } = "";

        //[Required, DataType(DataType.Password)]
        //public string NewPassword { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$",
         ErrorMessage = "Password must contain letters and numbers (min 8 chars).")]
        public string NewPassword { get; set; } = "";



        [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
        public string ConfirmNewPassword { get; set; } = "";
    }
}