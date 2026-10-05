using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Linq;
using WebHook.API.Data;
using WebHook.API.Models;
using WebHook.API.Services;

namespace WebHook.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebHookController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly TrelloService _trello;
        private readonly CardRequestMessageQueueService _queue;

        public WebHookController(AppDbContext db, TrelloService trello, CardRequestMessageQueueService queue)
        {
            _db = db;
            _trello = trello;
            _queue = queue;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonElement payload)
        {
            // Try to extract card id from common Trello webhook shapes
            string? cardId = null;

            if (payload.TryGetProperty("action", out var action) && action.ValueKind == JsonValueKind.Object)
            {
                if (action.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    if (data.TryGetProperty("card", out var cardJson) && cardJson.ValueKind == JsonValueKind.Object)
                    {
                        if (cardJson.TryGetProperty("id", out var idProp)) cardId = idProp.GetString();
                        if (string.IsNullOrEmpty(cardId) && cardJson.TryGetProperty("shortLink", out var sl)) cardId = sl.GetString();
                    }
                }
            }

            // fallback: try top-level card
            if (string.IsNullOrEmpty(cardId) && payload.TryGetProperty("card", out var topCard) && topCard.ValueKind == JsonValueKind.Object)
            {
                if (topCard.TryGetProperty("id", out var idProp2)) cardId = idProp2.GetString();
            }

            if (string.IsNullOrEmpty(cardId)) return BadRequest("Card id not found in payload");

            var card = await _trello.GetCardAsync(cardId) ?? payload;

            // Read fields
            string name = card.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
            string desc = card.TryGetProperty("desc", out var d) ? d.GetString() ?? string.Empty : string.Empty;
            string link = card.TryGetProperty("shortUrl", out var l) ? l.GetString() ?? string.Empty : string.Empty;

            // Determine type
            var type = DetermineType(name, desc);

            bool isNew = true;

            if (type == CardType.Project)
            {
                // create or update project
                var existing = await _db.Projects.GetByCardIdAsync(cardId);
                if (existing != null)
                {
                    existing.Name = name;
                    existing.Description = desc;
                    existing.Link = link;
                    isNew = false;
                    await _db.Projects.UpdateAsync(existing);
                    await _db.SaveChangesAsync();
                    await _queue.EnqueueAsync(new QueueMessage { Id = existing.ProjectId, CardType = CardType.Project, IsNew = isNew });
                    return Ok();
                }

                // parse aliases from description header line Aliases:
                var aliases = ParseAliases(desc);

                var project = new ProjectEntity { CardId = cardId, Name = name, Description = desc, Aliases = aliases, Link = link };
                await _db.Projects.AddAsync(project);
                await _db.SaveChangesAsync();
                await _queue.EnqueueAsync(new QueueMessage { Id = project.ProjectId, CardType = CardType.Project, IsNew = true });
                return Ok();
            }

            if (type == CardType.Feature)
            {
                // find project by id or alias
                var projectId = FindProjectIdFromText(desc);
                if (projectId == null)
                {
                    var alias = FindProjectAliasFromText(desc);
                    if (!string.IsNullOrEmpty(alias))
                    {
                        var proj = await _db.Projects.FindByAliasAsync(alias);
                        if (proj != null) projectId = proj.ProjectId;
                    }
                }

                if (projectId == null) return BadRequest("Could not determine parent project for feature");

                var existing = await _db.Features.GetByCardIdAsync(cardId);
                if (existing != null)
                {
                    existing.Name = name;
                    existing.Description = desc;
                    existing.Link = link;
                    isNew = false;
                    await _db.Features.UpdateAsync(existing);
                    await _db.SaveChangesAsync();
                    await _queue.EnqueueAsync(new QueueMessage { Id = existing.FeatureId, CardType = CardType.Feature, IsNew = isNew });
                    return Ok();
                }

                var aliases = ParseAliases(desc);
                var feature = new FeatureEntity { CardId = cardId, Name = name, Description = desc, Aliases = aliases, Link = link, ProjectId = projectId.Value };
                await _db.Features.AddAsync(feature);
                await _db.SaveChangesAsync();
                await _queue.EnqueueAsync(new QueueMessage { Id = feature.FeatureId, CardType = CardType.Feature, IsNew = true });
                return Ok();
            }

            // Task
            {
                int? featureId = FindFeatureIdFromText(desc);
                int? projectId = null;
                if (featureId != null)
                {
                    var f = await _db.Features.GetByIdAsync(featureId.Value);
                    projectId = f?.ProjectId;
                }
                else
                {
                    projectId = FindProjectIdFromText(desc);
                    if (projectId == null)
                    {
                        var alias = FindFeatureAliasFromText(desc);
                        if (!string.IsNullOrEmpty(alias))
                        {
                            var feat = await _db.Features.FindByAliasAsync(alias);
                            if (feat != null) { featureId = feat.FeatureId; projectId = feat.ProjectId; }
                        }
                    }
                }

                var existing = await _db.Tasks.GetByCardIdAsync(cardId);
                if (existing != null)
                {
                    existing.Name = name;
                    existing.Description = desc;
                    existing.Link = link;
                    existing.FeatureId = featureId;
                    existing.ProjectId = projectId;
                    isNew = false;
                    await _db.Tasks.UpdateAsync(existing);
                    await _db.SaveChangesAsync();
                    await _queue.EnqueueAsync(new QueueMessage { Id = existing.TaskId, CardType = CardType.Task, IsNew = isNew });
                    return Ok();
                }

                var task = new TaskEntity { CardId = cardId, Name = name, Description = desc, Link = link, FeatureId = featureId, ProjectId = projectId };
                await _db.Tasks.AddAsync(task);
                await _db.SaveChangesAsync();
                await _queue.EnqueueAsync(new QueueMessage { Id = task.TaskId, CardType = CardType.Task, IsNew = true });
                return Ok();
            }
        }

        private static CardType DetermineType(string name, string desc)
        {
            if (name.Contains("[Project]", StringComparison.OrdinalIgnoreCase) || desc.Contains("Type:Project", StringComparison.OrdinalIgnoreCase)) return CardType.Project;
            if (name.Contains("[Feature]", StringComparison.OrdinalIgnoreCase) || desc.Contains("Type:Feature", StringComparison.OrdinalIgnoreCase)) return CardType.Feature;
            return CardType.Task;
        }

        private static string? ParseAliases(string desc)
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

        private int? FindProjectIdFromText(string text)
        {
            // look for ProjectId:123
            if (string.IsNullOrEmpty(text)) return null;
            var parts = text.Split(new[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (p.StartsWith("ProjectId:", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(p.Substring(p.IndexOf(':') + 1), out var id)) return id;
                }
            }
            return null;
        }

        private int? FindFeatureIdFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var parts = text.Split(new[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (p.StartsWith("FeatureId:", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(p.Substring(p.IndexOf(':') + 1), out var id)) return id;
                }
            }
            return null;
        }

        private string? FindProjectAliasFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("ProjectAlias:", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(line.IndexOf(':') + 1).Trim();
                }
            }
            return null;
        }

        private string? FindFeatureAliasFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("FeatureAlias:", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(line.IndexOf(':') + 1).Trim();
                }
            }
            return null;
        }
    }
}
