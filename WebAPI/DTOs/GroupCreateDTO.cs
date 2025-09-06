using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public class GroupCreateDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
