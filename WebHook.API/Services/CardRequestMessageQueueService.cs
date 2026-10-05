using Dapper;
using WebHook.API.Models;
using WebHook.API.Data;

namespace WebHook.API.Services
{
    // SQLite-backed queue: writes a record to CardRequestMessageQueue table.
    public class CardRequestMessageQueueService
    {
        private readonly AppDbContext _db;

        public CardRequestMessageQueueService(AppDbContext db)
        {
            _db = db;
        }

        public async Task EnqueueAsync(QueueMessage message)
        {
            // Try to map incoming message to QueueMessage shape
            int entityId = 0;
            CardType cardType = CardType.Task;
            bool isNew = true;

            if (message is QueueMessage qm)
            {
                entityId = qm.Id;
                cardType = qm.CardType;
                isNew = qm.IsNew;
            }

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            var sql = "INSERT INTO CardRequestMessageQueue (CardType, IsNew, EntityId) VALUES (@CardType, @IsNew, @EntityId);";
            await conn.ExecuteAsync(sql, new { CardType = (int)cardType, IsNew = isNew ? 1 : 0, EntityId = entityId });
        }
    }
}
