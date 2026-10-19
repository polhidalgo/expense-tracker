using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.Expenses;

[Collection(DatabaseCollection.Name)]
public sealed class UpdateAndDeleteExpenseTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    private static object UpdateRequest(decimal amount = 99.95m, Guid? categoryId = null) => new
    {
        amount,
        currency = "AUD",
        expenseDate = "2026-10-20",
        categoryId = categoryId ?? CategorySeed.Dining,
        merchant = "  New merchant  ",
        description = "New description"
    };

    // AC-11 / FR-EXP-004: every editable field changes; id, owner, createdAt and source do not.
    [Fact]
    public async Task Update_ChangesEditableFields_Only()
    {
        var original = await AddExpenseAsync(TestUsers.UserA, ExpenseSource.AiReceipt);
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.PutAsJsonAsync($"/api/expenses/{original.Id}", UpdateRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        var fetched = await client.GetFromJsonAsync<ExpenseResponse>($"/api/expenses/{original.Id}");

        foreach (var expense in new[] { updated, fetched })
        {
            Assert.NotNull(expense);
            Assert.Equal(99.95m, expense.Amount);
            Assert.Equal(new DateOnly(2026, 10, 20), expense.ExpenseDate);
            Assert.Equal(CategorySeed.Dining, expense.CategoryId);
            Assert.Equal("Dining", expense.CategoryName);
            Assert.Equal("New merchant", expense.Merchant);
            Assert.Equal("New description", expense.Description);

            Assert.Equal(original.Id, expense.Id);
            Assert.Equal(TestUsers.UserA, expense.UserId);
            Assert.Equal("AiReceipt", expense.Source);
            Assert.Equal(original.CreatedAt, expense.CreatedAt, TimeSpan.FromMilliseconds(1));
        }
    }

    // AC-12: an invalid amount (400) or a nonexistent category (404) leaves the stored expense unchanged.
    [Theory]
    [InlineData(-5, false, HttpStatusCode.BadRequest)]
    [InlineData(12.345, false, HttpStatusCode.BadRequest)]
    [InlineData(20, true, HttpStatusCode.NotFound)]
    public async Task Update_Invalid_LeavesExpenseUnchanged(decimal amount, bool unknownCategory, HttpStatusCode expected)
    {
        var original = await AddExpenseAsync(TestUsers.UserA);
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.PutAsJsonAsync($"/api/expenses/{original.Id}",
            UpdateRequest(amount, unknownCategory ? Guid.NewGuid() : null));

        Assert.Equal(expected, response.StatusCode);
        await AssertUnchangedAsync(original);
    }

    // FR-EXP-004: another user cannot update the expense; it looks like it does not exist.
    [Fact]
    public async Task Update_OtherUsersExpense_Returns404_AndChangesNothing()
    {
        var original = await AddExpenseAsync(TestUsers.UserA);
        using var client = Factory.CreateClientFor(TestUsers.UserB);

        var response = await client.PutAsJsonAsync($"/api/expenses/{original.Id}", UpdateRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertUnchangedAsync(original);
    }

    // AC-13 / FR-EXP-005: only the targeted expense is deleted.
    [Fact]
    public async Task Delete_Owned_Returns204_AndDeletesOnlyThatExpense()
    {
        var target = await AddExpenseAsync(TestUsers.UserA);
        var kept = await AddExpenseAsync(TestUsers.UserA);
        var otherUsers = await AddExpenseAsync(TestUsers.UserB);
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.DeleteAsync($"/api/expenses/{target.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(
            new[] { kept.Id, otherUsers.Id }.Order(),
            (await db.Expenses.Select(e => e.Id).ToListAsync()).Order());
        Assert.Equal(10, await db.Categories.CountAsync());
    }

    // AC-14: an unknown id, or another user's expense → 404, and nothing is deleted.
    [Fact]
    public async Task Delete_UnknownOrOtherUsers_Returns404_AndDeletesNothing()
    {
        var expense = await AddExpenseAsync(TestUsers.UserA);
        using var owner = Factory.CreateClientFor(TestUsers.UserA);
        using var other = Factory.CreateClientFor(TestUsers.UserB);

        var unknown = await owner.DeleteAsync($"/api/expenses/{Guid.NewGuid()}");
        var foreign = await other.DeleteAsync($"/api/expenses/{expense.Id}");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        await AssertUnchangedAsync(expense);
    }

    private async Task<Expense> AddExpenseAsync(Guid userId, ExpenseSource source = ExpenseSource.Manual)
    {
        var expense = new Expense(userId, CategorySeed.Groceries, 42.50m, "AUD",
            new DateOnly(2026, 10, 18), "Woolworths", "Weekly groceries", source);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        return expense;
    }

    private async Task AssertUnchangedAsync(Expense original)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.Expenses.AsNoTracking().SingleAsync(e => e.Id == original.Id);

        Assert.Equal(original.UserId, stored.UserId);
        Assert.Equal(original.CategoryId, stored.CategoryId);
        Assert.Equal(original.Amount, stored.Amount);
        Assert.Equal(original.Currency, stored.Currency);
        Assert.Equal(original.ExpenseDate, stored.ExpenseDate);
        Assert.Equal(original.Merchant, stored.Merchant);
        Assert.Equal(original.Description, stored.Description);
        Assert.Equal(original.Source, stored.Source);
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
