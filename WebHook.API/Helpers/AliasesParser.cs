namespace WebHook.API.Helpers
{
    public static class AliasesParser
    {
        public static string? Parse(string desc)
        {
            return Parse(desc, "Aliases:");
        }

        public static string? FindProjectAliasFromText(string text)
        {
            return Parse(text, "ProjectAlias:");
        }

        public static string? FindFeatureAliasFromText(string text)
        {
            return Parse(text, "FeatureAlias:");
        }

        private static string? Parse(string desc, string pattern)
        {
            // look for line starting with Aliases: a;b;c
            var lines = desc?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(line.IndexOf(':') + 1).Trim();
                }
            }
            return null;
        }
    }
}
