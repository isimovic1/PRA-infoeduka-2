
using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{

    //DTO used during user login,Contains only email and password, both required to authenticate.
    // Validates that email format is correct and password is provided.
    public class LoginDTO
    {

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Incorrect email.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
    }
}
