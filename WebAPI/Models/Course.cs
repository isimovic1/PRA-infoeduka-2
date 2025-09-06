using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Course
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? ShortDescription { get; set; }

    public virtual ICollection<CourseStudent> CourseStudents { get; set; } = new List<CourseStudent>();

    public virtual ICollection<CourseTeacher> CourseTeachers { get; set; } = new List<CourseTeacher>();

    public virtual ICollection<FileAsset> FileAssets { get; set; } = new List<FileAsset>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
