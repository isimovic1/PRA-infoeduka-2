using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.AdminUsers
{
    public class UserEditVM
    {
        public int Id { get; set; }

        [EmailAddress, MaxLength(256)]
        public string Email { get; set; } = ""; // read-only in UI

        [Required, MaxLength(50)]
        public string FirstName { get; set; } = "";

        [Required, MaxLength(50)]
        public string LastName { get; set; } = "";

        [Range(0, 2)]
        public byte Role { get; set; }  // 0 Student, 1 Professor, 2 Admin

        public int? GroupId { get; set; }  // req for students, null for admins

        public bool IsFirstLogin { get; set; }
    }
}
