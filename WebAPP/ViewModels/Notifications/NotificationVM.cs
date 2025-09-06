namespace WebApp.ViewModels.Notifications
{
    public class NotificationVM
    {
        public int Id { get; set; }
        public int ToUserId { get; set; }
        public int? FromUserId { get; set; }
        public string Title { get; set; } = "";
        public string? Body { get; set; }
        public string? Link { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? FromUserEmail { get; set; }
    }
}
