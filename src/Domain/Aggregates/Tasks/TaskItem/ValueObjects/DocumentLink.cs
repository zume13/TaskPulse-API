using Domain.Aggregates.Tasks.Task.Errors;
using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.Tasks.Task.ValueObjects
{
    public sealed record DocumentLink
    {
        public const int MaxLabelLength = 200;

        private static readonly HashSet<string> AllowedHosts =
            new(StringComparer.OrdinalIgnoreCase)
            {
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
            };

        private static readonly string[] GoogleDocPrefixes =
        [
            "/document/d/",
            "/spreadsheets/d/",
            "/presentation/d/"
        ];

        // First path segments that are GitHub site pages, not account names.
        // Needed because /settings/profile looks exactly like /owner/repo.
        private static readonly HashSet<string> ReservedGitHubOwners =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "login", "logout", "signup", "join", "settings", "explore",
                "orgs", "organizations", "marketplace", "features",
                "notifications", "sponsors", "topics", "pricing", "search",
                "new", "apps", "enterprise", "collections", "trending",
                "about", "security", "codespaces", "pulls", "issues",
                "dashboard"
            };

        // What may follow /owner/repo. Anything else is rejected.
        private static readonly HashSet<string> AllowedGitHubSections =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "blob", "tree", "issues", "pull", "pulls", "wiki",
                "discussions", "releases", "commit"
            };

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

            if (!string.IsNullOrEmpty(parsed.UserInfo) || !parsed.IsDefaultPort)
                return TaskDocumentErrors.InvalidUrl;

            // Uri.Host is already lower-cased and the set is case-insensitive.
            // Exact match only, so "evil-docs.google.com.attacker.com" or
            // arbitrary subdomains are rejected.
            if (!AllowedHosts.Contains(parsed.Host))
                return TaskDocumentErrors.InvalidHost;

            if (!HasValidPath(parsed))
                return TaskDocumentErrors.InvalidAbsolutePath;

            if (string.IsNullOrWhiteSpace(rawLabel))
                return GeneralErrors.General.Empty(nameof(rawLabel));

            var label = rawLabel.Trim();

            if (label.Length > MaxLabelLength)
                return TaskDocumentErrors.LabelTooLong;

            // AbsoluteUri keeps the escaped form; ToString() can unescape characters.
            return new DocumentLink(parsed.AbsoluteUri, label);
        }

        /// <remarks>
        /// The host has already passed the allow-list when this runs, so only
        /// hosts that need a stricter path rule are special-cased. Every other
        /// allowed host (Microsoft 365 / Office / OneDrive) just needs to point
        /// at a real path rather than the site root.
        /// </remarks>
        private static bool HasValidPath(Uri uri)
        {
            var host = uri.Host;
            var path = uri.AbsolutePath;

            if (host.Equals("docs.google.com", StringComparison.OrdinalIgnoreCase))
            {
                return GoogleDocPrefixes.Any(prefix =>
                    path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && path.Length > prefix.Length);
            }

            if (host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {
                var segments = path.Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries);

                // Needs at least /owner/repo, and the "owner" must not be a
                // GitHub site page such as /settings or /orgs.
                if (segments.Length < 2
                    || ReservedGitHubOwners.Contains(segments[0]))
                    return false;

                // /owner/repo itself is fine. Anything deeper must be a
                // known repo section (blob, tree, issues, pull, ...).
                return segments.Length == 2
                       || AllowedGitHubSections.Contains(segments[2]);
            }

            return path.Length > 1;
        }
    }
}