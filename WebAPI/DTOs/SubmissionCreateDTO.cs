using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public class SubmissionCreateDto
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        public int FileAssetId { get; set; }
    }
}
