using QueueT.Notifications;

namespace AspNetCoreWebApp.Files
{
    [Notifications(DefaultMessageType = typeof(FileNotification))]
    public enum FileNotifications
    {
        Created,
        Updated,
        Deleted
    }
}