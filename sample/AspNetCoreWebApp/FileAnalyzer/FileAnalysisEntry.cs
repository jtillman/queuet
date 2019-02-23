using Newtonsoft.Json;

namespace AspNetCoreWebApp.FileAnalyzer
{
    public class EntryLocation
    {
        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("character")]
        public int Character { get; set; }
    }

    public class FileAnalysisEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("from")]
        public EntryLocation From { get; set; }

        [JsonProperty("to")]
        public EntryLocation To { get; set; }

        public static FileAnalysisEntry FromDiagnostic(Microsoft.CodeAnalysis.Diagnostic diagnostic)
        {
            var lineSpan = diagnostic.Location.GetLineSpan();
            return new FileAnalysisEntry
            {
                Id = diagnostic.Id,
                Severity = diagnostic.Severity.ToString(),
                Description = diagnostic.GetMessage(),
                From = new EntryLocation { Line = lineSpan.StartLinePosition.Line, Character = lineSpan.StartLinePosition.Character },
                To = new EntryLocation { Line = lineSpan.EndLinePosition.Line, Character = lineSpan.EndLinePosition.Character }
            };
        }
    }
}
