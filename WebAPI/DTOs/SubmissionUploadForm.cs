
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public class SubmissionUploadForm
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        public IFormFile File { get; set; } = default!;
    }
}
