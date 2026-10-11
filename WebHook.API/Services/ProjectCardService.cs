using WebHook.API.Data;
using WebHook.API.Data.Repositories;
using WebHook.API.Helpers;
using WebHook.API.HttpClients;
using WebHook.API.Models;

namespace WebHook.API.Services
{
    public class ProjectCardService : CardService<ProjectCard>
    {
        public ProjectCardService(ILogger<ProjectCardService> logger,
            AppDbContext db,
            TrelloService trello,
            CardRequestMessageQueueService queue) : base(logger, db, trello, queue)
        {
            
        }

        public override async Task<bool> ProcessCard(ProjectCard projectCard)
        {
            if (projectCard == null)
            {
                _logger.LogWarning("ProjectCard is null");
                return false;
            }

            if (projectCard.ProjectId <= 0)
            {
                _logger.LogWarning("ProjectCard.ProjectId is less or equal to 0");
                return false;
            }

            var projectAliases = AliasesParser.FindProjectAliasFromText(projectCard.Description);

            var existing = await _db.Projects.GetByProjectCardIdAsync(projectCard.ProjectId);
            if (existing != null)
            {
                existing.Name = projectCard.Name;
                existing.Description = projectCard.Description;
                existing.Link = projectCard.Link;
                existing.Aliases = projectAliases;
                await _db.Projects.UpdateAsync(existing);
                await _db.SaveChangesAsync();
                await _queue.EnqueueAsync(new QueueMessage
                {
                    Id = existing.ProjectId,
                    CardType = CardType.Project,
                    IsNew = false
                });
                return true;
            }

            var project = new ProjectEntity
            {
                ProjectCardId = projectCard.ProjectId,
                TrelloCardId = projectCard.TrelloCardId,
                Name = projectCard.Name,
                Description = projectCard.Description,
                Aliases = aliases,
                Link = projectCard.Link
            };

            await _db.Projects.AddAsync(project);
            await _db.SaveChangesAsync();
            await _queue.EnqueueAsync(new QueueMessage
            {
                Id = project.ProjectId,
                CardType = CardType.Project,
                IsNew = true
            });

            return true;
        }
    }
}
