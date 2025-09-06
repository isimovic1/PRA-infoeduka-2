// WebAPI/DTOs/CourseTeacherDto.cs
namespace WebAPI.DTOs
{
    public  class CourseTeacherDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public bool IsAssistant { get; set; }
    }
}
