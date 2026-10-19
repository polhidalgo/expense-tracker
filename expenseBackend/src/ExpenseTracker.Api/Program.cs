using ExpenseTracker.Api;
using ExpenseTracker.Api.ErrorHandling;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
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

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();

// Synthetic local user, Development only (D-01). No ICurrentUser is registered
// in any other environment until real authentication exists (FR-AUTH-001).
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<ICurrentUser, LocalCurrentUser>();
}

var app = builder.Build();

// Errors are returned as ProblemDetails, without internal details.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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
