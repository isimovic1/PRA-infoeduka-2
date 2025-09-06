using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class ImportBatch
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedById { get; set; }

    public string SourceFileName { get; set; } = null!;

    public virtual User? CreatedBy { get; set; }

    public virtual ICollection<ImportRow> ImportRows { get; set; } = new List<ImportRow>();
}
