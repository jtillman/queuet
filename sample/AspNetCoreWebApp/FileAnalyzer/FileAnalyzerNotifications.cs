using QueueT.Notifications;

namespace AspNetCoreWebApp.FileAnalyzer
{
    [Notifications(DefaultMessageType = typeof(FileAnalyzerNotification))]
    public enum FileAnalyzerNotifications
    {
        Completed
    }
}
