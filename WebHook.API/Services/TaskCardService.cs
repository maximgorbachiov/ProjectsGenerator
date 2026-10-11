using Microsoft.CodeAnalysis;
using WebHook.API.Data;
using WebHook.API.Data.Repositories;
using WebHook.API.Helpers;
using WebHook.API.HttpClients;
using WebHook.API.Models;

namespace WebHook.API.Services
{
    public class TaskCardService : CardService<TaskCard>
    {
        public TaskCardService(ILogger<TaskCardService> logger,
            AppDbContext db,
            TrelloService trello,
            CardRequestMessageQueueService queue) : base(logger, db, trello, queue)
        {
        }

        public override async Task<bool> ProcessCard(TaskCard taskCard)
        {
            if (taskCard == null)
            {
                _logger.LogWarning("TaskCard is null");
                return false;
            }

            if (taskCard.TaskId <= 0)
            {
                _logger.LogWarning("TaskCard.TaskId is less or equal to 0");
                return false;
            }

            var existing = await _db.Tasks.GetByTaskCardIdAsync(taskCard.TaskId);
            if (existing != null)
            {
                existing.Name = taskCard.Name;
                existing.Description = taskCard.Description;
                existing.Link = taskCard.Link;

                await _db.Tasks.UpdateAsync(existing);
                await _db.SaveChangesAsync();
                await _queue.EnqueueAsync(new QueueMessage
                {
                    Id = existing.TaskId,
                    CardType = CardType.Task,
                    IsNew = false
                });

                return true;
            }

            int? projectId = IdParser.FindProjectIdFromText(taskCard.Description);
            if (!projectId.HasValue)
            {
                var projectAlias = AliasesParser.FindProjectAliasFromText(taskCard.Description);
                if (!string.IsNullOrEmpty(projectAlias))
                {
                    var project = await _db.Projects.FindByAliasAsync(projectAlias);
                    if (project != null)
                    {
                        projectId = project.ProjectId;
                    }
                    else
                    {
                        _logger.LogWarning($"Task {taskCard.TaskId} can not be associated with Project. Project alias {projectAlias} not found", projectAlias);
                        return false;
                    }
                }
            }

            int? featureId = IdParser.FindFeatureIdFromText(taskCard.Description);
            if (!featureId.HasValue)
            {
                var featureAlias = AliasesParser.FindFeatureAliasFromText(taskCard.Description);
                if (!string.IsNullOrEmpty(featureAlias))
                {
                    var feature = await _db.Features.FindByAliasAsync(featureAlias);
                    if (feature != null)
                    {
                        featureId = feature.FeatureId;
                    }
                    else
                    {
                        _logger.LogWarning($"Task {taskCard.TaskId} can not be associated with Feature. Feature alias {featureAlias} not found", featureAlias);
                        return false;
                    }
                }
            }

            var task = new TaskEntity
            {
                TaskCardId = taskCard.TaskId,
                TrelloCardId = taskCard.TrelloCardId,
                Name = taskCard.Name,
                Description = taskCard.Description,
                Link = taskCard.Link,
                FeatureId = featureId,
                ProjectId = projectId
            };
            await _db.Tasks.AddAsync(task);
            await _db.SaveChangesAsync();
            await _queue.EnqueueAsync(new QueueMessage
            {
                Id = task.TaskId,
                CardType = CardType.Task,
                IsNew = true
            });

            return true;
        }
    }
}
