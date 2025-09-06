
namespace WebAPI.DTOs
{
    public  class AddUserToGroupDTO
    {
        public int? GroupId { get; set; }  // null = remove from group
    }
}
