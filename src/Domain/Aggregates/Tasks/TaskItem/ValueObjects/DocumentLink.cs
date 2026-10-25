
using Domain.Aggregates.Tasks.Task.Errors;
using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.Tasks.Task.ValueObjects
{
    public sealed record DocumentLink
    {
        public const int MaxLabelLength = 200;

        private static readonly string[] AllowedHosts =
        [
            "docs.google.com",
            "word.cloud.microsoft",
            "excel.cloud.microsoft",
            "powerpoint.cloud.microsoft",
            "word.office.com",
            "excel.office.com",
            "powerpoint.office.com",
            "office.com",
            "microsoft365.com",
            "1drv.ms",
            "github.com"
        ];

        public string Url { get; }
        public string Label { get; }

        private DocumentLink(string url, string label)
        {
            Url = url;
            Label = label;
        }

        public static ResultT<DocumentLink> Create(
            string? rawUrl,
            string? rawLabel)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
                return GeneralErrors.General.Empty(nameof(rawUrl));

            if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var parsed))
                return TaskDocumentErrors.InvalidUrl;

            if (parsed.Scheme != Uri.UriSchemeHttps)
                return TaskDocumentErrors.InvalidUriScheme;

            if (!string.IsNullOrEmpty(parsed.UserInfo) ||
                !parsed.IsDefaultPort)
                return TaskDocumentErrors.InvalidUrl;

            var host = parsed.Host;

            if (!IsAllowedHost(host))
                return TaskDocumentErrors.InvalidHost;

            if (!HasValidPath(parsed))
                return TaskDocumentErrors.InvalidAbsolutePath;

            if (string.IsNullOrWhiteSpace(rawLabel))
                return GeneralErrors.General.Empty(nameof(rawLabel));

            var label = rawLabel.Trim();

            if (label.Length > MaxLabelLength)
                return TaskDocumentErrors.LabelTooLong;

            return new DocumentLink(parsed.ToString(), label);
        }

        private static bool IsAllowedHost(string host)
        {
            return AllowedHosts.Any(allowedHost =>
                string.Equals(
                    host,
                    allowedHost,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasValidPath(Uri uri)
        {
            var host = uri.Host;
            var path = uri.AbsolutePath;

            if (host.Equals(
                    "docs.google.com",
                    StringComparison.OrdinalIgnoreCase))
            {
                return path.StartsWith(
                           "/document/d/",
                           StringComparison.OrdinalIgnoreCase)
                       || path.StartsWith(
                           "/spreadsheets/d/",
                           StringComparison.OrdinalIgnoreCase)
                       || path.StartsWith(
                           "/presentation/d/",
                           StringComparison.OrdinalIgnoreCase);
            }

            if (host.EndsWith(
                    ".microsoft.com",
                    StringComparison.OrdinalIgnoreCase)
                || host.Equals(
                    "office.com",
                    StringComparison.OrdinalIgnoreCase)
                || host.Equals(
                    "microsoft365.com",
                    StringComparison.OrdinalIgnoreCase))
            {
                return path != "/";
            }
            if (host.Equals(
                    "1drv.ms",
                    StringComparison.OrdinalIgnoreCase))
            {
                return path.Length > 1;
            }

            if (host.Equals(
                    "github.com",
                    StringComparison.OrdinalIgnoreCase))
            {
                var segments = path
                    .Split(
                        '/',
                        StringSplitOptions.RemoveEmptyEntries);

                return segments.Length >= 2
                       && !segments[0].Equals(
                           "login",
                           StringComparison.OrdinalIgnoreCase)
                       && !segments[0].Equals(
                           "settings",
                           StringComparison.OrdinalIgnoreCase)
                       && !segments[0].Equals(
                           "signup",
                           StringComparison.OrdinalIgnoreCase)
                       && !segments[0].Equals(
                           "explore",
                           StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
