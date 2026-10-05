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

        public async Task<TaskEntity?> GetByCardIdAsync(string cardId)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<TaskEntity>("SELECT * FROM Tasks WHERE CardId = @cardId", new { cardId });
        }

        public async Task<TaskEntity?> GetByIdAsync(int id)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<TaskEntity>("SELECT * FROM Tasks WHERE TaskId = @id", new { id });
        }

        public async Task AddAsync(TaskEntity entity)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            var sql = "INSERT INTO Tasks (FeatureId, ProjectId, CardId, Name, Description, Link) VALUES (@FeatureId, @ProjectId, @CardId, @Name, @Description, @Link); SELECT last_insert_rowid();";
            var id = await conn.ExecuteScalarAsync<long>(sql, entity);
            entity.TaskId = (int)id;
        }

        public async Task UpdateAsync(TaskEntity entity)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            var sql = "UPDATE Tasks SET FeatureId=@FeatureId, ProjectId=@ProjectId, CardId=@CardId, Name=@Name, Description=@Description, Link=@Link WHERE TaskId=@TaskId";
            await conn.ExecuteAsync(sql, entity);
        }
    }
}
