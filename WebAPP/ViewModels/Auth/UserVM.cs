namespace WebApp.ViewModels.Auth
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public int Role { get; set; }          // 0 Student, 1 Professor, 2 Admin
        public string RoleName { get; set; } = "";
        public int? GroupId { get; set; }
        public bool IsFirstLogin { get; set; }
    }
}
