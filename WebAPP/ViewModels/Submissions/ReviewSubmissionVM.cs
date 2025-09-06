using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.Submissions
{
    public class ReviewSubmissionVM
    {
        [Required] public int SubmissionId { get; set; }
        public bool Reviewed { get; set; } = true;
        public string? Comment { get; set; }
    }
}