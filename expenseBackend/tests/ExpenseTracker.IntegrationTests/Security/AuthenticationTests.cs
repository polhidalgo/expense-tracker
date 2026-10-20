using System.Net;
using System.Net.Http.Headers;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.Security;

[Collection(DatabaseCollection.Name)]
public sealed class AuthenticationTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    // Every private endpoint needs a signed-in user.
    [Theory]
    [InlineData("GET", "/api/expenses")]
    [InlineData("POST", "/api/expenses")]
    [InlineData("GET", "/api/categories")]
    [InlineData("GET", "/api/dashboard/summary?startDate=2026-10-01&endDate=2026-10-31")]
    [InlineData("POST", "/api/receipts/analyze")]
    public async Task PrivateEndpoint_WithoutUser_Returns401(string method, string path)
    {
        using var client = Factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_WithoutUser_Returns200()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // With the real Supabase validation, a missing or made-up token is rejected.
    [Theory]
    [InlineData(null)]
    [InlineData("eyJhbGciOiJFUzI1NiJ9.eyJzdWIiOiIxMjMifQ.bm90LWEtc2lnbmF0dXJl")]
    public async Task RealAuthentication_MissingOrMadeUpToken_Returns401(string? token)
    {
        await using var factory = Database.CreateRealAuthenticationFactory();
        using var client = factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/expenses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // RLS is on, so Supabase's Data API (publishable key) cannot read the tables.
    [Fact]
    public async Task RowLevelSecurity_IsEnabledOnAllTables()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tablesWithoutRls = await db.Database
            .SqlQueryRaw<string>("""
                SELECT c.relname AS "Value"
                FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'public' AND c.relkind = 'r' AND NOT c.relrowsecurity
                  AND c.relname IN ('Expenses', 'Categories', 'AppSecrets', '__EFMigrationsHistory')
                """)
            .ToListAsync();

        Assert.Empty(tablesWithoutRls);
    }
}
