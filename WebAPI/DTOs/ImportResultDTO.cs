namespace WebAPI.DTOs
{
    public  class ImportResultDto
    {
        public int BatchId { get; set; }
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<RowError> Errors { get; set; } = new();
        public sealed class RowError { public int RowNumber { get; set; } public string Error { get; set; } = ""; }
    }
}

