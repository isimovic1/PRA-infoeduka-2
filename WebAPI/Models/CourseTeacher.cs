using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class CourseTeacher
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public int TeacherId { get; set; }

    public bool IsAssistant { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual User Teacher { get; set; } = null!;
}
