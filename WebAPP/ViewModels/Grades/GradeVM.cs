namespace WebApp.ViewModels.Grades
{
    public class GradeVM
    {
        public int? Id { get; set; }
        public int SubmissionId { get; set; }
        public int? TeacherId { get; set; }
        public decimal? Points { get; set; }
        public DateTime? GradedAt { get; set; }
        public string? Note { get; set; }
    }
}
