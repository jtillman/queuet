using Microsoft.Azure.ServiceBus;
using QueueT.Brokers;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace QueueT.Tests.Brokers
{
    public class ServiceBusExtensionsTest
    {
        [Fact]
        public void ToServiceBusMessage_Converts_All_Information()
        {
            var queueTMessage = new QueueTMessage
            {
                Id = "123",
                ContentType = "contentType",
                Properties = new Dictionary<string, string> { { "prop1", "value1" } },
                EncodedBody = new byte[] { 1, 2, 3},
                Created = DateTime.UtcNow,
                MessageType = "messageType"
            };

            var sbMessage = queueTMessage.ToServiceBusMessage();

            Assert.Equal(queueTMessage.Id, sbMessage.MessageId);
            Assert.Equal(queueTMessage.ContentType, sbMessage.ContentType);
            Assert.Equal(queueTMessage.EncodedBody, sbMessage.Body);

            Assert.Equal(
                new Dictionary<string, object>
                {
                    { "prop1", "value1"},
                    { ServiceBusBroker.MessageTypeProperty, queueTMessage.MessageType }
                },
                sbMessage.UserProperties);

        }

        [Fact]
        public void ToQueueTMessage_Converts_All_Information()
        {
            var messageType = "messageType";

            var sbMessage = new Message
            {
                MessageId = "messageId",
                ContentType ="contentType",
                Body = new byte[] {10, 9, 8}
            };

            sbMessage.UserProperties[ServiceBusBroker.MessageTypeProperty] = messageType;
            sbMessage.UserProperties["prop1"] = "value1";

            var queueTMessage = sbMessage.ToQueueTMessage();

            Assert.Equal(sbMessage.MessageId, queueTMessage.Id);
            Assert.Equal(sbMessage.ContentType, queueTMessage.ContentType);
            Assert.Equal(sbMessage.Body, queueTMessage.EncodedBody);
            Assert.Equal(messageType, queueTMessage.MessageType);
            Assert.Equal(new Dictionary<string, string> { { "prop1", "value1" } }, queueTMessage.Properties);
        }
    }
}
