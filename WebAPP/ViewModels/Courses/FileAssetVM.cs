namespace WebApp.ViewModels.Courses
{
    public class FileAssetVM
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "application/octet-stream";
        public long SizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public int UploadedById { get; set; }
        public int CourseId { get; set; }
        public string? UploadedByName { get; set; } // optional if API returns it
    }
}
