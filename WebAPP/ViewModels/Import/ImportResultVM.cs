using System.Collections.Generic;

namespace WebApp.ViewModels.Import
{
    public class ImportResultVM
    {
        public int BatchId { get; set; }
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<RowErrorVM> Errors { get; set; } = new();

        public sealed class RowErrorVM
        {
            public int RowNumber { get; set; }
            public string Error { get; set; } = "";
        }
    }
}
