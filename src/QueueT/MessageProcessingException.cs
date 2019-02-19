﻿using System;

namespace QueueT
{

    public class MessageProcessingException : Exception
    {
        public MessageAction Action { get; }

        public MessageProcessingException(
            MessageAction action = MessageAction.Acknowledge,
            string message = null,
            Exception innerException = null) : base(message, innerException)
        {
            Action = action;
        }
    }
}
