namespace ZororoMemorialPark.Services
{
    /// <summary>
    /// Stores runtime-created public files outside the App Service deployment directory.
    /// On Azure App Service the location is %HOME%\data\zororo-static; locally it remains wwwroot.
    /// </summary>
    public sealed class PersistentStaticFileStorage
    {
        private static readonly HashSet<string> PublicDirectories = new(StringComparer.OrdinalIgnoreCase)
        {
            "uploads",
            "receipts",
            "documents"
        };

        private readonly string _webRootPath;

        public PersistentStaticFileStorage(IWebHostEnvironment environment)
        {
            _webRootPath = environment.WebRootPath;

            var configuredPath = Environment.GetEnvironmentVariable("UPLOADS_ROOT_PATH");
            var appServiceHome = Environment.GetEnvironmentVariable("HOME");
            var isAzureAppService = !string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));

            RootPath = !string.IsNullOrWhiteSpace(configuredPath)
                ? configuredPath
                : isAzureAppService && !string.IsNullOrWhiteSpace(appServiceHome)
                    ? Path.Combine(appServiceHome, "data", "zororo-static")
                    : _webRootPath;

            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public string GetDirectory(params string[] pathSegments)
        {
            var path = CombinePath(RootPath, pathSegments);
            Directory.CreateDirectory(path);
            return path;
        }

        public string? GetPersistentFilePathForPublicPath(string? publicPath)
        {
            var segments = GetPublicPathSegments(publicPath);
            return segments is null ? null : CombinePath(RootPath, segments);
        }

        public string? GetExistingFilePathForPublicPath(string? publicPath)
        {
            var persistentPath = GetPersistentFilePathForPublicPath(publicPath);
            if (persistentPath is null)
                return null;

            if (File.Exists(persistentPath) || string.Equals(RootPath, _webRootPath, StringComparison.OrdinalIgnoreCase))
                return persistentPath;

            // During the one-time migration, files already deployed under wwwroot remain readable.
            var segments = GetPublicPathSegments(publicPath)!;
            var legacyPath = CombinePath(_webRootPath, segments);
            return File.Exists(legacyPath) ? legacyPath : persistentPath;
        }

        private static string[]? GetPublicPathSegments(string? publicPath)
        {
            if (string.IsNullOrWhiteSpace(publicPath))
                return null;

            var pathOnly = publicPath.Split('?', '#')[0].Trim().TrimStart('/');
            var segments = pathOnly.Split('/', StringSplitOptions.RemoveEmptyEntries);

            return segments.Length > 0 && PublicDirectories.Contains(segments[0])
                ? segments
                : null;
        }

        private static string CombinePath(string rootPath, IEnumerable<string> pathSegments)
        {
            var path = rootPath;

            foreach (var segment in pathSegments)
            {
                if (string.IsNullOrWhiteSpace(segment) ||
                    segment is "." or ".." ||
                    segment.Contains(Path.DirectorySeparatorChar) ||
                    segment.Contains(Path.AltDirectorySeparatorChar))
                {
                    throw new InvalidOperationException("An invalid file storage path was requested.");
                }

                path = Path.Combine(path, segment);
            }

            return path;
        }
    }
}
