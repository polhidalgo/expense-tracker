using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.FixedExpenses;

[Collection(DatabaseCollection.Name)]
public sealed class FixedExpenseEndpointTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    private sealed record FixedExpenseResponse(
        Guid Id,
        Guid CategoryId,
        string Name,
        decimal Amount,
        string Currency,
        DateOnly StartDate,
        string Frequency);

    private static object Request(
        string name = "Rent",
        decimal amount = 500m,
        Guid? categoryId = null,
        string frequency = "Monthly") => new
    {
        name,
        amount,
        currency = "AUD",
        categoryId = categoryId ?? CategorySeed.Groceries,
        startDate = "2026-10-01",
        frequency
    };

    // Create, list, update and delete; nothing is ever written to Expenses.
    [Fact]
    public async Task FixedExpense_CreateListUpdateDelete()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var created = await client.PostAsJsonAsync("/api/fixed-expenses", Request());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var fixedExpense = await created.Content.ReadFromJsonAsync<FixedExpenseResponse>();
        Assert.Equal("Rent", fixedExpense!.Name);
        Assert.Equal("Monthly", fixedExpense.Frequency);

        var list = await client.GetFromJsonAsync<List<FixedExpenseResponse>>("/api/fixed-expenses");
        Assert.Equal([fixedExpense], list);

        var updated = await client.PutAsJsonAsync($"/api/fixed-expenses/{fixedExpense.Id}",
            Request(name: "Gym", amount: 25.50m, categoryId: CategorySeed.Dining, frequency: "Fortnightly"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var afterUpdate = Assert.Single((await client.GetFromJsonAsync<List<FixedExpenseResponse>>("/api/fixed-expenses"))!);
        Assert.Equal(fixedExpense.Id, afterUpdate.Id);
        Assert.Equal("Gym", afterUpdate.Name);
        Assert.Equal(25.50m, afterUpdate.Amount);
        Assert.Equal(CategorySeed.Dining, afterUpdate.CategoryId);
        Assert.Equal("Fortnightly", afterUpdate.Frequency);

        var deleted = await client.DeleteAsync($"/api/fixed-expenses/{fixedExpense.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<FixedExpenseResponse>>("/api/fixed-expenses"))!);

        Assert.Equal(0, await CountAsync(db => db.Expenses));
    }

    // UserB cannot see, update or delete UserA's fixed expense; it looks like it does not exist.
    [Fact]
    public async Task FixedExpense_OfOtherUser_Returns404_AndChangesNothing()
    {
        using var clientA = Factory.CreateClientFor(TestUsers.UserA);
        using var clientB = Factory.CreateClientFor(TestUsers.UserB);
        var created = await clientA.PostAsJsonAsync("/api/fixed-expenses", Request());
        var fixedExpense = await created.Content.ReadFromJsonAsync<FixedExpenseResponse>();

        Assert.Empty((await clientB.GetFromJsonAsync<List<FixedExpenseResponse>>("/api/fixed-expenses"))!);

        var update = await clientB.PutAsJsonAsync($"/api/fixed-expenses/{fixedExpense!.Id}", Request(name: "Changed"));
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);

        var delete = await clientB.DeleteAsync($"/api/fixed-expenses/{fixedExpense.Id}");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        var stillThere = Assert.Single((await clientA.GetFromJsonAsync<List<FixedExpenseResponse>>("/api/fixed-expenses"))!);
        Assert.Equal("Rent", stillThere.Name);
    }

    [Fact]
    public async Task UpdateOrDelete_UnknownId_Returns404()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);
        var unknownId = Guid.NewGuid();

        var update = await client.PutAsJsonAsync($"/api/fixed-expenses/{unknownId}", Request());
        var delete = await client.DeleteAsync($"/api/fixed-expenses/{unknownId}");

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownCategory_Returns404_AndChangesNothing()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);
        var created = await client.PostAsJsonAsync("/api/fixed-expenses", Request());
        var fixedExpense = await created.Content.ReadFromJsonAsync<FixedExpenseResponse>();

        var response = await client.PutAsJsonAsync($"/api/fixed-expenses/{fixedExpense!.Id}",
            Request(name: "Changed", categoryId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var stored = Assert.Single((await client.GetFromJsonAsync<List<FixedExpenseResponse>>("/api/fixed-expenses"))!);
        Assert.Equal(fixedExpense, stored);
    }

    [Fact]
    public async Task Create_UnknownCategory_Returns404_AndSavesNothing()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.PostAsJsonAsync("/api/fixed-expenses", Request(categoryId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await CountAsync(db => db.FixedExpenses));
    }

    [Theory]
    [InlineData("", 500, "Monthly")]
    [InlineData("Rent", 0, "Monthly")]
    [InlineData("Rent", 12.345, "Monthly")]
    [InlineData("Rent", 500, "Daily")]
    public async Task Create_Invalid_Returns400_AndSavesNothing(string name, decimal amount, string frequency)
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.PostAsJsonAsync("/api/fixed-expenses", Request(name, amount, frequency: frequency));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountAsync(db => db.FixedExpenses));
    }

    // An invalid change keeps the fixed expense as it was.
    [Theory]
    [InlineData("", 500, "Monthly")]
    [InlineData("Changed", 0, "Monthly")]
    [InlineData("Changed", 500, "Daily")]
    public async Task Update_Invalid_Returns400_AndChangesNothing(string name, decimal amount, string frequency)
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);
        var created = await client.PostAsJsonAsync("/api/fixed-expenses", Request());
        var fixedExpense = await created.Content.ReadFromJsonAsync<FixedExpenseResponse>();

        var response = await client.PutAsJsonAsync($"/api/fixed-expenses/{fixedExpense!.Id}",
            Request(name, amount, frequency: frequency));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = Assert.Single((await client.GetFromJsonAsync<List<FixedExpenseResponse>>("/api/fixed-expenses"))!);
        Assert.Equal(fixedExpense, stored);
    }

    private async Task<int> CountAsync<T>(Func<AppDbContext, IQueryable<T>> table)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await table(db).CountAsync();
    }
}
