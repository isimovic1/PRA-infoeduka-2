using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.Grades
{
    public class GradeSaveVM
    {
        [Required] public int SubmissionId { get; set; }
        [Required, Range(0, 100)] public decimal Points { get; set; }
        public string? Note { get; set; }

        // for redirect back to course
        [Required] public int CourseId { get; set; }
    }
}
