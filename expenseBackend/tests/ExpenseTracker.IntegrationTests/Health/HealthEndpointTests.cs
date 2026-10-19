using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.IntegrationTests.Infrastructure;

namespace ExpenseTracker.IntegrationTests.Health;

[Collection(DatabaseCollection.Name)]
public sealed class HealthEndpointTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    // Phase 1 exit criterion: the API can connect to the isolated test database.
    [Fact]
    public async Task Health_Database_ReturnsConnected_AgainstTestDatabase()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/health/database");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DatabaseHealthResponse>();
        Assert.Equal("Connected", body?.Status);
    }

    private sealed record DatabaseHealthResponse(string Status);
}
