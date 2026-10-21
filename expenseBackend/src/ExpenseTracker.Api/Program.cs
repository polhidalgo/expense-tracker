using ExpenseTracker.Api;
using ExpenseTracker.Api.ErrorHandling;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Infrastructure.AI;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.Infrastructure.Security;
using ExpenseTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Enums as names ("Weekly"); numbers and unknown names are rejected with 400.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
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
builder.Services.AddScoped<ISummaryService, SummaryService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IFixedExpenseService, FixedExpenseService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();

builder.Services.AddHttpClient<GroqReceiptExtractor>(client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Supabase Auth signs the access tokens; the public keys come from the project's discovery document.
var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException(
        "Supabase URL is missing. Set 'Supabase:Url' (e.g. https://<project>.supabase.co).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{supabaseUrl.TrimEnd('/')}/auth/v1";
        options.Audience = "authenticated";
        options.MapInboundClaims = false;
    });

// Every endpoint needs a signed-in user unless it says otherwise.
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var app = builder.Build();

if (args is [SetGroqKeyCommand.Name])
{
    await SetGroqKeyCommand.RunAsync(app.Services);
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapControllers();

app.MapGet("/api/health", () =>
    Results.Ok(new
    {
        status = "Healthy",
        application = "ExpenseTracker.Api"
    }))
    .AllowAnonymous();

app.MapGet("/api/health/database",
    async (AppDbContext db) =>
    {
        var connected = await db.Database.CanConnectAsync();

        return connected
            ? Results.Ok(new { status = "Connected" })
            : Results.Problem(
                "Database connection unavailable",
                statusCode: 503);
    })
    .AllowAnonymous();

app.Run();

public partial class Program { }
