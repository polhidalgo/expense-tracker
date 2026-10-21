using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.Budgets;

[Collection(DatabaseCollection.Name)]
public sealed class BudgetEndpointTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    private sealed record BudgetResponse(decimal? WeeklyAmount);

    // Set, read, change, remove, and read null again.
    [Fact]
    public async Task Budget_SetGetChangeDelete()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var set = await client.PutAsJsonAsync("/api/budget", new { weeklyAmount = 100m });
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        Assert.Equal(100m, (await set.Content.ReadFromJsonAsync<BudgetResponse>())!.WeeklyAmount);
        Assert.Equal(100m, (await client.GetFromJsonAsync<BudgetResponse>("/api/budget"))!.WeeklyAmount);

        var changed = await client.PutAsJsonAsync("/api/budget", new { weeklyAmount = 250.75m });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(250.75m, (await client.GetFromJsonAsync<BudgetResponse>("/api/budget"))!.WeeklyAmount);

        var deleted = await client.DeleteAsync("/api/budget");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Null((await client.GetFromJsonAsync<BudgetResponse>("/api/budget"))!.WeeklyAmount);
        Assert.Equal(0, await CountBudgetsAsync());
    }

    [Fact]
    public async Task Get_WithoutBudget_ReturnsNull()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var budget = await client.GetFromJsonAsync<BudgetResponse>("/api/budget");

        Assert.Null(budget!.WeeklyAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(12.345)]
    public async Task Put_InvalidAmount_Returns400_AndSavesNothing(decimal amount)
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.PutAsJsonAsync("/api/budget", new { weeklyAmount = amount });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountBudgetsAsync());
    }

    // An invalid change keeps the budget the user already had.
    [Fact]
    public async Task Put_InvalidAmount_KeepsExistingBudget()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);
        await client.PutAsJsonAsync("/api/budget", new { weeklyAmount = 100m });

        var response = await client.PutAsJsonAsync("/api/budget", new { weeklyAmount = 0m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(100m, (await client.GetFromJsonAsync<BudgetResponse>("/api/budget"))!.WeeklyAmount);
    }

    // UserB cannot see or remove UserA's budget.
    [Fact]
    public async Task Budget_IsOnlyForTheCaller()
    {
        using var clientA = Factory.CreateClientFor(TestUsers.UserA);
        using var clientB = Factory.CreateClientFor(TestUsers.UserB);
        await clientA.PutAsJsonAsync("/api/budget", new { weeklyAmount = 100m });

        Assert.Null((await clientB.GetFromJsonAsync<BudgetResponse>("/api/budget"))!.WeeklyAmount);
        await clientB.DeleteAsync("/api/budget");

        Assert.Equal(100m, (await clientA.GetFromJsonAsync<BudgetResponse>("/api/budget"))!.WeeklyAmount);
    }

    private async Task<int> CountBudgetsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Budgets.CountAsync();
    }
}
