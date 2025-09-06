namespace WebApp.ViewModels.AdminCourses
{
    public class CourseStudentItemVM
    {
        public int UserId { get; set; }
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public int? GroupId { get; set; }
    }
}
