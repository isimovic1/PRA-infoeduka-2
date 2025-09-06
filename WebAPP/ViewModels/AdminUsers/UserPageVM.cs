using System.Collections.Generic;

namespace WebApp.ViewModels.AdminUsers
{
    public class UsersPageVM
    {
        public int Total { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public List<UserListItemVM> Items { get; set; } = new();
        public string? Search { get; set; }
        public byte? Role { get; set; }
    }
}
