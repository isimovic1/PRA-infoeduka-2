using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class ImportRow
{
    public int Id { get; set; }

    public int ImportBatchId { get; set; }

    public int RowNumber { get; set; }

    public string Data { get; set; } = null!;

    public bool IsSuccess { get; set; }

    public string? Error { get; set; }

    public virtual ImportBatch ImportBatch { get; set; } = null!;
}
