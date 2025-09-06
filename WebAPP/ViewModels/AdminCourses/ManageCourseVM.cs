using System.Collections.Generic;
using WebApp.ViewModels.Courses;

namespace WebApp.ViewModels.AdminCourses
{
    public class ManageCourseVM
    {
        public CourseVM Course { get; set; } = new();

        public List<CourseTeacherItemVM> Teachers { get; set; } = new();
        public List<CourseStudentItemVM> Students { get; set; } = new();

        // optional search boxes on the page
        public string? QProf { get; set; }
        public string? QStud { get; set; }

        public List<UserSearchResultVM> ProfResults { get; set; } = new();
        public List<UserSearchResultVM> StudResults { get; set; } = new();
    }
}
