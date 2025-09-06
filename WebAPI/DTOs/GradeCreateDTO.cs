using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public class GradeCreateDTO
    {
        [Range(0, 100)]
        public decimal Points { get; set; }
        public string? Note { get; set; }
    }
}
