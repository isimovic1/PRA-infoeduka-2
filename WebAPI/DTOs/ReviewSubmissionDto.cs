namespace WebAPI.DTOs
{
    public sealed class ReviewSubmissionDto
    {
        public bool Reviewed { get; set; } = true;
        public string? Comment { get; set; } 
    }
}
