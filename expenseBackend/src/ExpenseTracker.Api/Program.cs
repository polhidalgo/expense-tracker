
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.UseCors("Frontend");

app.MapControllers();

app.MapGet("/api/health", () =>
    Results.Ok(new
    {
        status = "Healthy",
        application = "ExpenseTracker.Api"
    }));

app.MapGet("/api/health/database",
    async (AppDbContext db) =>
    {
        var connected = await db.Database.CanConnectAsync();

        return connected
            ? Results.Ok(new { status = "Connected" })
            : Results.Problem(
                "Database connection unavailable",
                statusCode: 503);
    });

app.Run();

public partial class Program { }
