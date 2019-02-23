using System;

namespace AspNetCoreWebApp.Files
{
    public class File
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Content { get; set; }

        public DateTime Created { get; set; }

        public DateTime LastModified { get; set; }

        public string Hash { get; set; }

        public bool Locked { get; set; }
    }
}
