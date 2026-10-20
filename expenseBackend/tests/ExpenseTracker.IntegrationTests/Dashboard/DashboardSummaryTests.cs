using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.Dashboard;

[Collection(DatabaseCollection.Name)]
public sealed class DashboardSummaryTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    // Totals match the user's expenses in the range, boundaries included.
    [Fact]
    public async Task Summary_TotalsOnlyCurrentUsersExpensesInRange()
    {
        await AddExpensesAsync(
            Expense(TestUsers.UserA, "2026-09-30", CategorySeed.Groceries, 100.00m),
            Expense(TestUsers.UserA, "2026-10-01", CategorySeed.Groceries, 12.50m),
            Expense(TestUsers.UserA, "2026-10-15", CategorySeed.Dining, 30.25m),
            Expense(TestUsers.UserA, "2026-10-20", CategorySeed.Groceries, 7.30m),
            Expense(TestUsers.UserA, "2026-10-31", CategorySeed.Dining, 0.95m),
            Expense(TestUsers.UserA, "2026-11-01", CategorySeed.Dining, 200.00m),
            Expense(TestUsers.UserB, "2026-10-15", CategorySeed.Groceries, 500.00m));

        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var summary = await client.GetFromJsonAsync<SummaryResponse>(
            "/api/dashboard/summary?startDate=2026-10-01&endDate=2026-10-31");

        Assert.NotNull(summary);

        var total = Assert.Single(summary.Totals);
        Assert.Equal("AUD", total.Currency);
        Assert.Equal(51.00m, total.Total);
        Assert.Equal(4, total.Count);

        Assert.Equal(2, summary.ByCategory.Length);
        Assert.Equal(31.20m, summary.ByCategory.Single(c => c.CategoryId == CategorySeed.Dining).Total);
        Assert.Equal(19.80m, summary.ByCategory.Single(c => c.CategoryId == CategorySeed.Groceries).Total);

        Assert.Equal(
            [new DateOnly(2026, 10, 31), new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 1)],
            summary.Recent.Select(r => r.ExpenseDate));
    }

    [Fact]
    public async Task Summary_RecentHoldsAtMostFiveExpenses()
    {
        var firstDay = new DateOnly(2026, 10, 1);
        await AddExpensesAsync(Enumerable.Range(0, 7)
            .Select(i => Expense(TestUsers.UserA, firstDay.AddDays(i).ToString("yyyy-MM-dd"), CategorySeed.Other, 1m))
            .ToArray());

        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var summary = await client.GetFromJsonAsync<SummaryResponse>(
            "/api/dashboard/summary?startDate=2026-10-01&endDate=2026-10-31");

        Assert.NotNull(summary);
        Assert.Equal(5, summary.Recent.Length);
        Assert.Equal(firstDay.AddDays(6), summary.Recent[0].ExpenseDate);
    }

    [Fact]
    public async Task Summary_NoExpenses_ReturnsEmptyTotals()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var summary = await client.GetFromJsonAsync<SummaryResponse>(
            "/api/dashboard/summary?startDate=2020-01-01&endDate=2020-01-31");

        Assert.NotNull(summary);
        Assert.Empty(summary.Totals);
        Assert.Empty(summary.ByCategory);
        Assert.Empty(summary.Recent);
    }

    [Theory]
    [InlineData("startDate=2026-10-31&endDate=2026-10-01", "startDate")]
    [InlineData("endDate=2026-10-31", "startDate")]
    [InlineData("startDate=2026-10-01", "endDate")]
    public async Task Summary_InvalidDates_Returns400(string queryString, string field)
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.GetAsync($"/api/dashboard/summary?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(field), $"Expected an error for '{field}'.");
    }

    private static Expense Expense(Guid userId, string date, Guid categoryId, decimal amount) =>
        new(userId, categoryId, amount, "AUD", DateOnly.Parse(date), null, null, ExpenseSource.Manual);

    private async Task AddExpensesAsync(params Expense[] expenses)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Expenses.AddRange(expenses);
        await db.SaveChangesAsync();
    }

    private sealed record SummaryResponse(TotalResponse[] Totals, CategoryResponse[] ByCategory, RecentResponse[] Recent);

    private sealed record TotalResponse(string Currency, decimal Total, int Count);

    private sealed record CategoryResponse(Guid CategoryId, string CategoryName, string Currency, decimal Total);

    private sealed record RecentResponse(Guid Id, DateOnly ExpenseDate);
}
