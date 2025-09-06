using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Submission
{
    public int Id { get; set; }

    public int FileAssetId { get; set; }

    public int CourseId { get; set; }

    public int StudentId { get; set; }

    public bool Reviewed { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual FileAsset FileAsset { get; set; } = null!;

    public virtual Grade? Grade { get; set; }

    public virtual User Student { get; set; } = null!;
}
