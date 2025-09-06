using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public class GroupUpdateDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
