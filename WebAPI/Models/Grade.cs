using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Grade
{
    public int Id { get; set; }

    public int SubmissionId { get; set; }

    public int TeacherId { get; set; }

    public decimal Points { get; set; }

    public DateTime GradedAt { get; set; }

    public virtual Submission Submission { get; set; } = null!;

    public virtual User Teacher { get; set; } = null!;
}
