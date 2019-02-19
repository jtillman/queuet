using System;

namespace QueueT.Notifications
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class SubscriptionAttribute : Attribute
    {
        public Enum Notification { get; }

        public string Queue { get; set; }

        public SubscriptionAttribute(object notification){
            Notification = notification as Enum;
        }
    }
}
