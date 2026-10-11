using Dapper;
using Microsoft.Data.Sqlite;
using WebHook.API.Models;

namespace WebHook.API.Data.Repositories
{
    public class TaskRepository
    {
        private readonly AppDbContext _ctx;
        public TaskRepository(AppDbContext ctx) => _ctx = ctx;

        private SqliteConnection CreateConnection() => _ctx.CreateConnection();

        public async Task<TaskEntity?> GetByTaskCardIdAsync(int taskCardId)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<TaskEntity>("SELECT TOP(1) * FROM Tasks WHERE TaskCardId = @taskCardId", new { taskCardId });
        }

        public async Task<TaskEntity?> GetByTrelloCardIdAsync(string trelloCardId)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<TaskEntity>("SELECT TOP(1) * FROM Tasks WHERE TrelloCardId = @trelloCardId", new { trelloCardId });
        }

        public async Task<TaskEntity?> GetByIdAsync(int id)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<TaskEntity>("SELECT TOP(1) * FROM Tasks WHERE TaskId = @id", new { id });
        }

        public async Task AddAsync(TaskEntity entity)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            var sql = "INSERT INTO Tasks (FeatureId, ProjectId, TaskCardId, TrelloCardId, Name, Description, Link) VALUES (@FeatureId, @ProjectId, @TaskCardId, @TrelloCardId, @Name, @Description, @Link); SELECT last_insert_rowid();";
            var id = await conn.ExecuteScalarAsync<long>(sql, entity);
            entity.TaskId = (int)id;
        }

        public async Task UpdateAsync(TaskEntity entity)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            var sql = "UPDATE Tasks SET Name=@Name, Description=@Description, Link=@Link WHERE TaskId=@TaskId";
            await conn.ExecuteAsync(sql, entity);
        }
    }
}
