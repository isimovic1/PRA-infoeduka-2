using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.Import
{
    public class UploadUsersVM
    {
        [Required]
        [Display(Name = "Excel file (.xls or .xlsx)")]
        public IFormFile File { get; set; } = default!;
    }
}
