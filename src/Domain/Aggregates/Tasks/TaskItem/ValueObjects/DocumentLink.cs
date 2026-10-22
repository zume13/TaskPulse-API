using Domain.Aggregates.Tasks.Task.Errors;
using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.Tasks.Task.ValueObjects
{
    public sealed record DocumentLink
    {
        public const int MaxLabelLength = 200;
        private static readonly string[] AllowedHosts = ["docs.google.com"];
        public string Url { get; }
        public string Label { get; }
        private DocumentLink(string url, string label)
        {
            Url = url;
            Label = label;
        }

        public static ResultT<DocumentLink> Create(string? rawUrl, string? rawLabel)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
                return GeneralErrors.General.Empty(nameof(rawUrl));

            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var parsed))
                return TaskDocumentErrors.InvalidUrl;
           
            if (parsed.Scheme != Uri.UriSchemeHttps)
                return TaskDocumentErrors.InvalidUriScheme;   

            if (!string.Equals(
                    parsed.Host,
                    "docs.google.com",
                    StringComparison.OrdinalIgnoreCase))
                return TaskDocumentErrors.InvalidHost;
            
            if (!parsed.AbsolutePath.StartsWith(
                    "/document/d/",
                    StringComparison.OrdinalIgnoreCase))
                return TaskDocumentErrors.InvalidAbsolutePath;
      
            if(string.IsNullOrWhiteSpace(rawLabel))
                return GeneralErrors.General.Empty(nameof(rawLabel));

            var label = rawLabel.Trim();

            if (label.Length > MaxLabelLength)
                return TaskDocumentErrors.LabelTooLong;

            return new DocumentLink(parsed.ToString(), label);
        }
    }
}
