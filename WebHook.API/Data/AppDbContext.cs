using Dapper;
using Microsoft.Data.Sqlite;
using WebHook.API.Data.Repositories;

namespace WebHook.API.Data
{
    // A lightweight Dapper-based replacement for EF DbContext used by the application.
    // It exposes Db sets that support Add/Update + LINQ enumeration and a Database facade
    // with EnsureCreated to initialize SQLite schema.
    public class AppDbContext
    {
        private readonly string _connectionString;

        public AppDbContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default") ?? "Data Source=webhook.db";
            Projects = new ProjectRepository(this);
            Features = new FeatureRepository(this);
            Tasks = new TaskRepository(this);
            Database = new DatabaseFacade(this);
        }

        public ProjectRepository Projects { get; }
        public FeatureRepository Features { get; }
        public TaskRepository Tasks { get; }

        public DatabaseFacade Database { get; }

        internal SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        // Repositories operate immediately; SaveChangesAsync is kept for compatibility.
        public Task SaveChangesAsync() => Task.CompletedTask;

        public class DatabaseFacade
        {
            private readonly AppDbContext _ctx;
            public DatabaseFacade(AppDbContext ctx) => _ctx = ctx;

            // Ensure tables exist for the simple entities used in the project.
            public void EnsureCreated()
            {
                using var conn = _ctx.CreateConnection();
                conn.Open();
                // Create tables if they don't exist. Keep schema simple and compatible with existing models.
                conn.Execute(@"CREATE TABLE IF NOT EXISTS Projects (
                    ProjectId INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectCardId INTEGER,
                    TrelloCardId TEXT,
                    Name TEXT,
                    Aliases TEXT,
                    Description TEXT,
                    Link TEXT);");

                conn.Execute(@"CREATE TABLE IF NOT EXISTS Features (
                    FeatureId INTEGER PRIMARY KEY AUTOINCREMENT,
                    FeatureCardId INTEGER,
                    ProjectId INTEGER NOT NULL,
                    TrelloCardId TEXT,
                    Aliases TEXT,
                    Name TEXT,
                    Description TEXT,
                    Link TEXT);");

                conn.Execute(@"CREATE TABLE IF NOT EXISTS Tasks (
                    TaskId INTEGER PRIMARY KEY AUTOINCREMENT,
                    TaskCardId INTEGER, 
                    FeatureId INTEGER,
                    ProjectId INTEGER,
                    TrelloCardId TEXT,
                    Name TEXT,
                    Description TEXT,
                    Link TEXT);");

                conn.Execute(@"CREATE TABLE IF NOT EXISTS CardRequestMessageQueue (
                    CardRequestMessageId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CardType INTEGER NOT NULL,
                    IsNew INTEGER NOT NULL,
                    EntityId INTEGER NOT NULL);");
            }
        }


    }
}
