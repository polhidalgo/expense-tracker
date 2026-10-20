using ExpenseTracker.Api;
using ExpenseTracker.Api.ErrorHandling;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Infrastructure.AI;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.Infrastructure.Security;
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

var encryptionKey = builder.Configuration["Secrets:EncryptionKey"];
if (string.IsNullOrWhiteSpace(encryptionKey))
{
    throw new InvalidOperationException(
        "Encryption key is missing. Set 'Secrets:EncryptionKey' (32 random bytes in base64) with " +
        "'dotnet user-secrets set \"Secrets:EncryptionKey\" \"<base64>\" --project src/ExpenseTracker.Api'.");
}

builder.Services.AddSingleton(new SecretProtector(encryptionKey));
builder.Services.AddScoped<SecretStore>();

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<GroqReceiptExtractor>(client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Local user for Development only. No ICurrentUser is registered elsewhere
// until real authentication exists.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<ICurrentUser, LocalCurrentUser>();
}

var app = builder.Build();

if (args is [SetGroqKeyCommand.Name])
{
    await SetGroqKeyCommand.RunAsync(app.Services);
    return;
}

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
