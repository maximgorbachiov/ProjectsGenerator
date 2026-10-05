using System.Text.Json;
using WebHook.API.Data;
using WebHook.API.Models;

namespace WebHook.API.Services
{
    public class ProjectCardService : CardService
    {
        private readonly AppDbContext _db;
        private readonly TrelloService _trello;
        private readonly CardRequestMessageQueueService _queue;

        public ProjectCardService(AppDbContext db, CardRequestMessageQueueService queue)
        {
            _db = db;
            _queue = queue;
        }

        public async Task<string> GetProjectCardIdAsync(string cardId)
        {
            var card = await _trello.GetCardAsync(cardId) ?? default(JsonElement);

            // Read fields
            string name = card.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
            string desc = card.TryGetProperty("desc", out var d) ? d.GetString() ?? string.Empty : string.Empty;
            string link = card.TryGetProperty("shortUrl", out var l) ? l.GetString() ?? string.Empty : string.Empty;

            var existing = await _db.Projects.GetByCardIdAsync(cardId);
            if (existing != null)
            {
                existing.Name = name;
                existing.Description = desc;
                existing.Link = link;
                await _db.Projects.UpdateAsync(existing);
                await _db.SaveChangesAsync();
                await _queue.EnqueueAsync(new QueueMessage { Id = existing.ProjectId, CardType = CardType.Project, IsNew = false });
                return cardId;
            }

            // parse aliases from description header line Aliases:
            var aliases = ParseAliases(desc);

            var project = new ProjectEntity { CardId = cardId, Name = name, Description = desc, Aliases = aliases, Link = link };
            await _db.Projects.AddAsync(project);
            await _db.SaveChangesAsync();
            await _queue.EnqueueAsync(new QueueMessage { Id = project.ProjectId, CardType = CardType.Project, IsNew = true });

            return cardId;
        }
    }
}
