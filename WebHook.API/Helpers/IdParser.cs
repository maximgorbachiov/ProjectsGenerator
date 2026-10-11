namespace WebHook.API.Helpers
{
    public static class IdParser
    {
        public static int? FindProjectIdFromText(string text)
        {
            // look for ProjectId:id in the text
            return FindIdFromText(text, "ProjectId:");
        }

        public static int? FindFeatureIdFromText(string text)
        {
            // look for FeatureId:id in the text
            return FindIdFromText(text, "FeatureId:");
        }

        public static int? FindTaskIdFromText(string text)
        {
            // look for TaskId:integer in the text
            return FindIdFromText(text, "TaskId:");
        }

        private static int? FindIdFromText(string text, string pattern)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var parts = text.Split(new[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (p.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(p.Substring(p.IndexOf(':') + 1), out var id)) return id;
                }
            }
            return null;
        }
    }

}
