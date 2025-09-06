namespace WebApp.ViewModels.AdminCourses
{
    public class CourseTeacherItemVM
    {
        public int UserId { get; set; }
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public bool IsAssistant { get; set; }
    }
}
