namespace WebApp.ViewModels.Submissions
{
    public class SubmissionItemVM
    {
        public int Id { get; set; }
        public int FileAssetId { get; set; }
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        public bool Reviewed { get; set; }

        public DateTime UploadedAt { get; set; }
        public string FileName { get; set; } = "";
        public string? FileUrl { get; set; }

        // za profesora
        public string? StudentName { get; set; }
    }
}
