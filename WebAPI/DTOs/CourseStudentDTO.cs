// WebAPI/DTOs/CourseStudentDto.cs
namespace WebAPI.DTOs
{
    public class CourseStudentDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public int? GroupId { get; set; }
    }
}
