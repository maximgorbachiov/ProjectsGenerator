using System.Text.Json;
using WebHook.API.Models;

namespace WebHook.API.ModelBuilders
{
    public class CardModelBuilder
    {
        public static Card BuildCardModel(string trelloCardId, JsonElement card)
        {
            string id = card.TryGetProperty("id", out var i) ? i.GetString() ?? string.Empty : string.Empty;
            int cardId = int.TryParse(id, out var parsedId) ? parsedId : 0;
            string name = card.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
            string desc = card.TryGetProperty("desc", out var d) ? d.GetString() ?? string.Empty : string.Empty;
            string link = card.TryGetProperty("shortUrl", out var l) ? l.GetString() ?? string.Empty : string.Empty;

            // Determine type
            var type = DetermineType(name, desc);

            if (type == CardType.Project)
            {
                return new ProjectCard
                {
                    ProjectId = cardId,
                    TrelloCardId = trelloCardId,
                    Name = name,
                    Description = desc,
                    Link = link
                };
            }
            else if (type == CardType.Feature)
            {
                return new FeatureCard
                {
                    FeatureId = cardId,
                    TrelloCardId = trelloCardId,
                    Name = name,
                    Description = desc,
                    Link = link
                };
            }
            else if (type == CardType.Task)
            {
                return new TaskCard
                {
                    TaskId = cardId,
                    TrelloCardId = trelloCardId,
                    Name = name,
                    Description = desc,
                    Link = link
                };
            }

            return null;
        }

        private static CardType DetermineType(string name, string desc)
        {
            if (name.Contains("[Project]", StringComparison.OrdinalIgnoreCase) || desc.Contains("Type:Project", StringComparison.OrdinalIgnoreCase)) return CardType.Project;
            if (name.Contains("[Feature]", StringComparison.OrdinalIgnoreCase) || desc.Contains("Type:Feature", StringComparison.OrdinalIgnoreCase)) return CardType.Feature;
            if (name.Contains("[Task]", StringComparison.OrdinalIgnoreCase) || desc.Contains("Type:Task", StringComparison.OrdinalIgnoreCase)) return CardType.Task;
            return CardType.Unknown;
        }
    }
}
