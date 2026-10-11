using Dapper;
using Microsoft.Data.Sqlite;
using WebHook.API.Models;

namespace WebHook.API.Data.Repositories
{
    public class FeatureRepository
    {
        private readonly AppDbContext _ctx;
        public FeatureRepository(AppDbContext ctx) => _ctx = ctx;

        private SqliteConnection CreateConnection() => _ctx.CreateConnection();

        public async Task<FeatureEntity?> GetByFeatureCardIdAsync(int featureCardId)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<FeatureEntity>("SELECT TOP(1) * FROM Features WHERE FeatureCardId = @featureCardId", new { featureCardId });
        }

        public async Task<FeatureEntity?> GetByTrelloCardIdAsync(string trelloCardId)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<FeatureEntity>("SELECT TOP(1) * FROM Features WHERE TrelloCardId = @trelloCardId", new { trelloCardId });
        }

        public async Task<FeatureEntity?> GetByIdAsync(int id)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<FeatureEntity>("SELECT TOP(1) * FROM Features WHERE FeatureId = @id", new { id });
        }

        public async Task<FeatureEntity?> FindByAliasAsync(string alias)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            var sql = "SELECT TOP(1) * FROM Features WHERE instr(';' || lower(coalesce(Aliases,'')) || ';', ';' || lower(@alias) || ';') > 0";
            return await conn.QueryFirstOrDefaultAsync<FeatureEntity>(sql, new { alias });
        }

        public async Task AddAsync(FeatureEntity entity)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            var sql = "INSERT INTO Features (ProjectId, FeatureCardId, TrelloCardId, Aliases, Name, Description, Link) VALUES (@ProjectId, @FeatureCardId, @TrelloCardId, @Aliases, @Name, @Description, @Link); SELECT last_insert_rowid();";
            var id = await conn.ExecuteScalarAsync<long>(sql, entity);
            entity.FeatureId = (int)id;
        }

        public async Task UpdateAsync(FeatureEntity entity)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            var sql = "UPDATE Features SET Aliases=@Aliases, Name=@Name, Description=@Description, Link=@Link WHERE FeatureId=@FeatureId";
            await conn.ExecuteAsync(sql, entity);
        }
    }
}
