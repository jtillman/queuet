using System;
using System.Collections.Generic;

namespace QueueT
{

    public class QueueTMessage
    {
        public string Id { get; set; }

        public string ContentType { get; set; }

        public string MessageType { get; set; }

        public IDictionary<string, string> Properties { get; set; }

        public byte[] EncodedBody { get; set; }

        public DateTime Created { get; set; }
    }
}
