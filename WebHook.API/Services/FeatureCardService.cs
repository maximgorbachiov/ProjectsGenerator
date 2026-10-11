using WebHook.API.Data;
using WebHook.API.Data.Repositories;
using WebHook.API.Helpers;
using WebHook.API.HttpClients;
using WebHook.API.Models;

namespace WebHook.API.Services
{
    public class FeatureCardService : CardService<FeatureCard>
    {
        public FeatureCardService(ILogger<FeatureCardService> logger,
            AppDbContext db,
            TrelloService trello,
            CardRequestMessageQueueService queue) : base(logger, db, trello, queue)
        {
        }
        public override async Task<bool> ProcessCard(FeatureCard featureCard)
        {
            if (featureCard == null)
            {
                _logger.LogWarning("FeatureCard is null");
                return false;
            }

            if (featureCard.FeatureId <= 0)
            {
                _logger.LogWarning("FeatureCard.FeatureId is less or equal to 0");
                return false;
            }

            var featureAliases = AliasesParser.FindFeatureAliasFromText(featureCard.Description);

            var existing = await _db.Features.GetByFeatureCardIdAsync(featureCard.FeatureId);
            if (existing != null)
            {
                existing.Name = featureCard.Name;
                existing.Description = featureCard.Description;
                existing.Link = featureCard.Link;
                existing.Aliases = featureAliases;

                await _db.Features.UpdateAsync(existing);
                await _db.SaveChangesAsync();
                await _queue.EnqueueAsync(new QueueMessage
                {
                    Id = existing.FeatureId,
                    CardType = CardType.Feature,
                    IsNew = false
                });

                return true;
            }

            // find project by id or alias
            var projectId = IdParser.FindProjectIdFromText(featureCard.Description);
            if (!projectId.HasValue)
            {
                var projectAlias = AliasesParser.FindProjectAliasFromText(featureCard.Description);
                if (!string.IsNullOrEmpty(projectAlias))
                {
                    var project = await _db.Projects.FindByAliasAsync(projectAlias);
                    if (project != null)
                    {
                        projectId = project.ProjectId;
                    }
                    else
                    {
                        _logger.LogWarning($"Feature {featureCard.FeatureId} can not be associated with Project. Project alias {projectAlias} not found", projectAlias);
                        return false;
                    }
                }
            }

            var feature = new FeatureEntity
            {
                FeatureCardId = featureCard.FeatureId,
                TrelloCardId = featureCard.TrelloCardId,
                Name = featureCard.Name,
                Description = featureCard.Description,
                Aliases = featureAliases,
                Link = featureCard.Link,
                ProjectId = projectId.Value
            };
            await _db.Features.AddAsync(feature);
            await _db.SaveChangesAsync();
            await _queue.EnqueueAsync(new QueueMessage
            {
                Id = feature.FeatureId,
                CardType = CardType.Feature,
                IsNew = true
            });

            return true;
        }
    }
}
