using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class CourseStudent
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public int StudentId { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
