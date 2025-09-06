// WebAPI/DTOs/CourseTeacherAssignDto.cs
namespace WebAPI.DTOs
{
    public class CourseTeacherAssignDto
    {
        public int UserId { get; set; }
        public bool IsAssistant { get; set; } = false;
    }
}
