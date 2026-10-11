using WebHook.API.Data;
using WebHook.API.Data.Repositories;
using WebHook.API.HttpClients;
using WebHook.API.Interfaces;
using WebHook.API.Models;
using WebHook.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Db (Dapper-based AppDbContext)
builder.Services.AddScoped<AppDbContext>();

builder.Services.AddHttpClient<TrelloService>();
builder.Services.AddScoped<CardRequestMessageQueueService>();
builder.Services.AddTransient<ProjectCardService>();
builder.Services.AddTransient<FeatureCardService>();
builder.Services.AddTransient<TaskCardService>();
// Factory delegate: resolve a CardService implementation by card model Type
builder.Services.AddTransient<Func<Type, ICardService>>(sp => ct =>
{
    return ct switch
    {
        Type t when t == typeof(ProjectCard) => sp.GetRequiredService<ProjectCardService>(),
        Type t when t == typeof(FeatureCard) => sp.GetRequiredService<FeatureCardService>(),
        _ => sp.GetRequiredService<TaskCardService>(),
    };
});

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
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
