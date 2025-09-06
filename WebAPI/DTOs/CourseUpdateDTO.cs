// WebAPI/DTOs/CourseUpdateDto.cs
using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public sealed class CourseUpdateDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = "";

        [MaxLength(500)]
        public string? ShortDescription { get; set; }
    }
}
