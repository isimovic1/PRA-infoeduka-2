using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.Auth
{
    public class RegisterVM
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, StringLength(50)]
        public string FirstName { get; set; } = "";

        [Required, StringLength(50)]
        public string LastName { get; set; } = "";

        // 0 = Student, 1 = Professor, 2 = Admin
        [Range(0, 2)]
        public int Role { get; set; }

        // Only required for Student (Role = 0)
        public int? GroupId { get; set; }

        //[Required, DataType(DataType.Password)]
        //public string Password { get; set; } = "";
        [Required, DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$",
        ErrorMessage = "Password must contain letters and numbers (min 8 chars).")]
        public string Password { get; set; } = "";


        [Required, DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = "";
    }
}
