namespace WebAPI.DTOs
{
    public class SubmissionDto
    {
        public int Id { get; set; }
        public int FileAssetId { get; set; }
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        public bool Reviewed { get; set; }

        // convenience: bubble up a few file fields
        public DateTime UploadedAt { get; set; }
        public string FileName { get; set; } = "";
        public string? FileUrl { get; set; }
        public string? StudentName { get; set; }
    }
}
