namespace WebApp.ViewModels.AdminUsers
{
    public class UserListItemVM
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public byte Role { get; set; }          // 0/1/2
        public string RoleName { get; set; } = ""; // "Student" | "Professor" | "Admin"
        public int? GroupId { get; set; }
        public bool IsFirstLogin { get; set; }
    }
}
