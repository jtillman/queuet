using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace QueueT.Notifications
{
    public static class NotificationAttributeExtensions
    {

        public static NotificationSubscription RegisterSubscription(this NotificationOptions options, NotificationDefinition notification, MethodInfo method, string taskQueue = null)
        {
            var subscription = new NotificationSubscription(notification, method, taskQueue);
            options.NotificationSubscriptions.Add(subscription);
            return subscription;
        }

        public static NotificationSubscription RegisterSubscription(this NotificationOptions options, Enum notificationEnum, MethodInfo method, string taskQueue = null)
        {
            var notification = options.Notifications.First(definition => notificationEnum.Equals(definition.EnumValue));
            return options.RegisterSubscription(notification, method, taskQueue);
        }

        public static NotificationSubscription RegisterSubscription(this NotificationOptions options, MethodInfo method, string topic, string taskQueue = null)
        {
            var notification = options.Notifications.First(definition => definition.Topic == topic);
            return options.RegisterSubscription(notification, method, taskQueue);
        }

        public static void RegisterSubscriptionAttributes(this NotificationOptions options, params Assembly[] assemblies)
        {

            if (0 == assemblies.Length)
            {
                assemblies = new Assembly[1] { Assembly.GetCallingAssembly() };
            }

            assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass)
                .SelectMany(type=>type.GetMethods())
                .Select(methodInfo => new { methodInfo, attribute = methodInfo.GetCustomAttribute<SubscriptionAttribute>(false) })
                .Where(entry => null != entry.attribute)
                .ToList()
                .ForEach(entry => options.RegisterSubscription(entry.attribute.Notification, entry.methodInfo, entry.attribute.Queue));
        }
    }

    public static class NotificationExtensions
    {
        public const string NotificationEnumSuffix = "notifications";
        public const string NotificationNamePattern = "{notification}";
        public const string TopicNamePattern = "{topic}";

        public static void RegisterNotificationEnum(this NotificationOptions notificationOptions, Type notificationEnumType)
        {
            if (notificationEnumType == null)
            {
                throw new ArgumentNullException(nameof(notificationEnumType));
            }

            if (!notificationEnumType.IsEnum)
            {
                throw new ArgumentException("Notification Type must be an enumeration", nameof(notificationEnumType));
            }

            var notificationAttribute = notificationEnumType.GetCustomAttribute<NotificationsAttribute>(false);

            var enumClassName = notificationEnumType.Name.ToLower();
            if (enumClassName.Length > NotificationEnumSuffix.Length &&
                enumClassName.EndsWith(NotificationEnumSuffix, StringComparison.InvariantCultureIgnoreCase))
            {
                enumClassName = enumClassName.Substring(0, enumClassName.Length - NotificationEnumSuffix.Length);
            }

            var notificationTemplate = notificationAttribute?.TopicTemplate?.Trim().ToLower() ?? NotificationNamePattern;
            var notificationName = notificationTemplate.Replace(NotificationNamePattern, enumClassName);

            foreach(var value in Enum.GetValues(notificationEnumType))
            {
                var enumFieldName = value.ToString();

                var topicAttribute = notificationEnumType
                    .GetMember(enumFieldName)
                    .First()
                    .GetCustomAttribute<TopicAttribute>(false);

                var topicTemplate = topicAttribute?.Template?.Trim().ToLower() ?? TopicNamePattern;
                var topicName = topicTemplate.Replace(TopicNamePattern, enumFieldName.ToLower());

                var topic = $"{notificationName}.{topicName}";
                var topicMessageType = topicAttribute?.MessageType ?? notificationAttribute?.DefaultMessageType ?? typeof(object);

                notificationOptions.Notifications.Add(new NotificationDefinition(topic, topicMessageType, value as Enum));
            }
        }

        public static void RegisterNotificationAttributes(this NotificationOptions notificationOptions, params Assembly[] assemblies)
        {
            if (0 == assemblies.Length)
            {
                assemblies = new Assembly[1] { Assembly.GetCallingAssembly() };
            }

            assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsEnum)
                .Select(enumType => new { enumType, attribute = enumType.GetCustomAttribute<NotificationsAttribute>(false) })
                .Where(entry => null != entry.attribute)
                .ToList()
                .ForEach(entry => notificationOptions.RegisterNotificationEnum(entry.enumType));
        }

        public static QueueTServiceCollection ConfigureNotifications(this QueueTServiceCollection serviceCollection, Action<NotificationOptions> configure)
        {
            serviceCollection.Services.Configure<NotificationOptions>(options => configure(options));
            return serviceCollection;
        }
    }
}
