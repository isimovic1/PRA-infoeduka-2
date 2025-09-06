namespace WebApp.ViewModels.Groups
{
    public class GroupMemberVM
    {
        public int Id { get; set; }         // user id
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }
}