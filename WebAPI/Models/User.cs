using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class User
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public byte Role { get; set; }

    public int? GroupId { get; set; }

    public bool IsFirstLogin { get; set; }

    public virtual ICollection<CourseStudent> CourseStudents { get; set; } = new List<CourseStudent>();

    public virtual ICollection<CourseTeacher> CourseTeachers { get; set; } = new List<CourseTeacher>();

    public virtual ICollection<FileAsset> FileAssets { get; set; } = new List<FileAsset>();

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual Group? Group { get; set; }

    public virtual ICollection<ImportBatch> ImportBatches { get; set; } = new List<ImportBatch>();

    public virtual ICollection<Notification> NotificationFromUsers { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationToUsers { get; set; } = new List<Notification>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
