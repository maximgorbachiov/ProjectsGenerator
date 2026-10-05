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

        public async Task<FeatureEntity?> GetByCardIdAsync(string cardId)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<FeatureEntity>("SELECT * FROM Features WHERE CardId = @cardId", new { cardId });
        }

        public async Task<FeatureEntity?> GetByIdAsync(int id)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<FeatureEntity>("SELECT * FROM Features WHERE FeatureId = @id", new { id });
        }

        public async Task<FeatureEntity?> FindByAliasAsync(string alias)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            var sql = "SELECT * FROM Features WHERE instr(';' || lower(coalesce(Aliases,'')) || ';', ';' || lower(@alias) || ';') > 0";
            return await conn.QueryFirstOrDefaultAsync<FeatureEntity>(sql, new { alias });
        }

        public async Task AddAsync(FeatureEntity entity)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            var sql = "INSERT INTO Features (ProjectId, CardId, Aliases, Name, Description, Link) VALUES (@ProjectId, @CardId, @Aliases, @Name, @Description, @Link); SELECT last_insert_rowid();";
            var id = await conn.ExecuteScalarAsync<long>(sql, entity);
            entity.FeatureId = (int)id;
        }

        public async Task UpdateAsync(FeatureEntity entity)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            var sql = "UPDATE Features SET ProjectId=@ProjectId, CardId=@CardId, Aliases=@Aliases, Name=@Name, Description=@Description, Link=@Link WHERE FeatureId=@FeatureId";
            await conn.ExecuteAsync(sql, entity);
        }
    }
}
