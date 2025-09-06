using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.Submissions
{
    public class SubmissionUploadVM
    {
        [Required] public int CourseId { get; set; }
        [Required] public IFormFile File { get; set; } = default!;
        public string? Comment { get; set; }
    }
}
