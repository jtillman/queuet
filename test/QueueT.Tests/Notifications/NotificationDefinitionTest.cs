using QueueT.Notifications;
using System;
using Xunit;

namespace QueueT.Tests.Notifications
{

    public class NotificationDefinitionTest
    {
        [Fact]
        public void Constructor_Throws_On_Null_Topic()
        {
            Assert.Throws<ArgumentNullException>(
                () => new NotificationDefinition(
                    null,
                    typeof(TestNotification),
                    TestNotifications.Started));
        }

        [Fact]
        public void Constructor_Throws_On_Null_BodyType()
        {
            Assert.Throws<ArgumentNullException>(
                () => new NotificationDefinition(
                    "test.started",
                    null,
                    TestNotifications.Started));
        }

        [InlineData("test.started", typeof(TestNotification), TestNotifications.Started)]
        [InlineData("test.failed", typeof(TestNotification), null)]
        [Theory]
        public void Constructor_Allows_Valid_Input(string topic, Type bodyType, Enum enumValue)
        {
            var definition = new NotificationDefinition(topic, bodyType, enumValue);
            Assert.Equal(definition.Topic, topic);
            Assert.Equal(definition.BodyType, bodyType);
            Assert.Equal(definition.EnumValue, enumValue);
        }

    }
}
