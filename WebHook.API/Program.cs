var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Db (Dapper-based AppDbContext)
builder.Services.AddScoped<WebHook.API.Data.AppDbContext>();

builder.Services.AddHttpClient<WebHook.API.Services.TrelloService>();
builder.Services.AddScoped<WebHook.API.Services.QueueService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Ensure database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebHook.API.Data.AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
