using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class FileAsset
{
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string StoredPath { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    public int CourseId { get; set; }

    public int UploadedById { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual User UploadedBy { get; set; } = null!;
}
