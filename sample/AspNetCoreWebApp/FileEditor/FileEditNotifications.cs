using QueueT.Notifications;

namespace AspNetCoreWebApp.FileEditor
{
    [Notifications(DefaultMessageType = typeof(FileEditNotification))]
    public enum FileEditNotifications
    {
        Created,
        Updated,
        Committed,
        Deleted
    }
}
