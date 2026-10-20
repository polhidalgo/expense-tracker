using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.Expenses;

[Collection(DatabaseCollection.Name)]
public sealed class CreateAndGetExpenseTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    private static readonly Guid Groceries = Guid.Parse("00000000-0000-0000-0000-000000000101");

    private static object ValidRequest(decimal amount = 42.50m, string? currency = "AUD", Guid? categoryId = null) => new
    {
        amount,
        currency,
        expenseDate = "2026-10-18",
        categoryId = categoryId ?? Groceries,
        merchant = "Woolworths",
        description = "Weekly groceries"
    };

    // AC-01 / FR-EXP-001: a valid create returns 201 with Location, and GET returns the saved values.
    [Fact]
    public async Task Create_Valid_Returns201_AndCanBeReadBack()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.PostAsJsonAsync("/api/expenses", ValidRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.NotNull(created);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/expenses/{created.Id}", response.Headers.Location.ToString(), StringComparison.OrdinalIgnoreCase);

        var fetched = await client.GetFromJsonAsync<ExpenseResponse>(response.Headers.Location);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(TestUsers.UserA, fetched.UserId);
        Assert.Equal(Groceries, fetched.CategoryId);
        Assert.Equal("Groceries", fetched.CategoryName);
        Assert.Equal(42.50m, fetched.Amount);
        Assert.Equal("AUD", fetched.Currency);
        Assert.Equal(new DateOnly(2026, 10, 18), fetched.ExpenseDate);
        Assert.Equal("Woolworths", fetched.Merchant);
        Assert.Equal("Weekly groceries", fetched.Description);
        Assert.Equal("Manual", fetched.Source);
    }

    // AC-01 / FR-EXP-001: owner, source and creation time sent by the client are ignored.
    [Fact]
    public async Task Create_IgnoresOwnerSourceAndCreatedAt_FromBody()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.PostAsJsonAsync("/api/expenses", new
        {
            amount = 10m,
            expenseDate = "2026-10-18",
            categoryId = Groceries,
            userId = TestUsers.UserB,
            source = "AiReceipt",
            createdAt = "2000-01-01T00:00:00Z"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.NotNull(created);
        Assert.Equal(TestUsers.UserA, created.UserId);
        Assert.Equal("Manual", created.Source);
        Assert.Equal("AUD", created.Currency);
        Assert.True(created.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    // AC-02 / AC-03 / AC-04: invalid amount or currency → 400 with the field error, nothing saved.
    [Theory]
    [InlineData("0", "AUD", "amount")]
    [InlineData("-1", "AUD", "amount")]
    [InlineData("42.505", "AUD", "amount")]
    [InlineData("10", "USD", "currency")]
    public async Task Create_InvalidAmountOrCurrency_Returns400_AndSavesNothing(string amount, string currency, string field)
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.PostAsJsonAsync("/api/expenses", ValidRequest(decimal.Parse(amount, CultureInfo.InvariantCulture), currency));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(field), $"Expected an error for '{field}'.");
        Assert.Equal(0, await CountExpensesAsync());
    }

    // AC-05: a category that is not one of the fixed categories → 404, nothing saved.
    [Fact]
    public async Task Create_UnknownCategory_Returns404_AndSavesNothing()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.PostAsJsonAsync("/api/expenses", ValidRequest(categoryId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await CountExpensesAsync());
    }

    // AC-06 / AC-07 / FR-EXP-002: only the owner can read an expense; others get 404 with no data.
    [Fact]
    public async Task Get_IsOnlyVisibleToOwner()
    {
        using var owner = Factory.CreateClientFor(TestUsers.UserA);
        using var other = Factory.CreateClientFor(TestUsers.UserB);

        var created = await (await owner.PostAsJsonAsync("/api/expenses", ValidRequest()))
            .Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.NotNull(created);

        var ownerResponse = await owner.GetAsync($"/api/expenses/{created.Id}");
        var otherResponse = await other.GetAsync($"/api/expenses/{created.Id}");
        var unknownResponse = await owner.GetAsync($"/api/expenses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownResponse.StatusCode);

        var otherBody = await otherResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Woolworths", otherBody);
        Assert.DoesNotContain("42.5", otherBody);
    }

    // AC-17 / FR-EXP-006: with the database unreachable, POST → 503 without internal details.
    [Fact]
    public async Task Create_DatabaseUnavailable_Returns503_WithoutInternalDetails()
    {
        await using var unreachable = new ApiFactory(
            "Host=127.0.0.1;Port=1;Database=unreachable;Username=nobody;Password=secret-value;Timeout=2");
        using var client = unreachable.CreateClientFor(TestUsers.UserA);

        var response = await client.PostAsJsonAsync("/api/expenses", ValidRequest());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret-value", body);
        Assert.DoesNotContain("Host=", body);
        Assert.DoesNotContain("Npgsql", body);
        Assert.DoesNotContain(" at ", body);
    }

    private async Task<int> CountExpensesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Expenses.CountAsync();
    }

    private sealed record ExpenseResponse(
        Guid Id,
        Guid UserId,
        Guid CategoryId,
        string CategoryName,
        decimal Amount,
        string Currency,
        DateOnly ExpenseDate,
        string? Merchant,
        string? Description,
        string Source,
        DateTimeOffset CreatedAt);
}
