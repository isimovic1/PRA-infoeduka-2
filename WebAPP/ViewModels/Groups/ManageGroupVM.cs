using System.Collections.Generic;

namespace WebApp.ViewModels.Groups
{
    public class ManageGroupVM
    {
        public GroupVM Group { get; set; } = new();
        public List<GroupMemberVM> Members { get; set; } = new();
        public string? Q { get; set; }                     // search term for adding students
        public List<GroupMemberVM> SearchResults { get; set; } = new(); // candidates to add (students)
        public List<GroupVM> OtherGroups { get; set; } = new();         // for move dropdown
    }
}