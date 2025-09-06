// WebAPI/DTOs/CourseDto.cs
namespace WebAPI.DTOs
{
    public sealed class CourseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? ShortDescription { get; set; }
    }
}
