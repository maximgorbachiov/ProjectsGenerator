using Dapper;
using Microsoft.Data.Sqlite;
using WebHook.API.Models;

namespace WebHook.API.Data.Repositories
{
    public class ProjectRepository
    {
        private readonly AppDbContext _ctx;
        public ProjectRepository(AppDbContext ctx) => _ctx = ctx;

        private SqliteConnection CreateConnection() => _ctx.CreateConnection();

        public async Task<ProjectEntity?> GetByProjectCardIdAsync(int projectCardId)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<ProjectEntity>("SELECT TOP(1) * FROM Projects WHERE ProjectCardId = @projectCardId", new { projectCardId });
        }

        public async Task<ProjectEntity?> GetByTrelloCardIdAsync(string trelloCardId)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<ProjectEntity>("SELECT TOP(1) * FROM Projects WHERE TrelloCardId = @trelloCardId", new { trelloCardId });
        }

        public async Task<ProjectEntity?> GetByIdAsync(int id)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<ProjectEntity>("SELECT TOP(1) * FROM Projects WHERE ProjectId = @id", new { id });
        }

        public async Task<ProjectEntity?> FindByAliasAsync(string alias)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            // match alias as a semicolon-separated token (case-insensitive)
            var sql = "SELECT TOP(1) * FROM Projects WHERE instr(';' || lower(coalesce(Aliases,'')) || ';', ';' || lower(@alias) || ';') > 0";
            return await conn.QueryFirstOrDefaultAsync<ProjectEntity>(sql, new { alias });
        }

        public async Task AddAsync(ProjectEntity entity)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            var sql = "INSERT INTO Projects (ProjectCardId, TrelloCardId, Name, Aliases, Description, Link) VALUES (@ProjectCardId, @TrelloCardId, @Name, @Aliases, @Description, @Link); SELECT last_insert_rowid();";
            var id = await conn.ExecuteScalarAsync<long>(sql, entity);
            entity.ProjectId = (int)id;
        }

        public async Task UpdateAsync(ProjectEntity entity)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            var sql = "UPDATE Projects SET Name=@Name, Aliases=@Aliases, Description=@Description, Link=@Link WHERE ProjectId=@ProjectId";
            await conn.ExecuteAsync(sql, entity);
        }
    }
}
