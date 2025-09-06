using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Notification
{
    public int Id { get; set; }

    public int ToUserId { get; set; }

    public int? FromUserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Body { get; set; }

    public string? Link { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }

    public virtual User? FromUser { get; set; }

    public virtual User ToUser { get; set; } = null!;
}
