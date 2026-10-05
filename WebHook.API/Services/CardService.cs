namespace WebHook.API.Services
{
    public class CardService
    {
        protected string? ParseAliases(string desc)
        {
            // look for line starting with Aliases: a;b;c
            var lines = desc?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("Aliases:", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(line.IndexOf(':') + 1).Trim();
                }
            }
            return null;
        }
    }
}
