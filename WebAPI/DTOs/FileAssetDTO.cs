namespace WebAPI.DTOs
{
    public class FileAssetDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public long SizeBytes { get; set; }
        public int CourseId { get; set; }
        public int UploadedById { get; set; }
        public string? UploadedByEmail { get; set; }   // convenience
        public DateTime UploadedAt { get; set; }
     
    }
}
