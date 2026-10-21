using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.Summary;

[Collection(DatabaseCollection.Name)]
public sealed class MonthsSummaryTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    // 12 months ending at endMonth, current month included.
    [Fact]
    public async Task Months_ReturnsTheTwelveMonthsEndingAtEndMonth()
    {
        var months = await GetMonthsAsync(TestUsers.UserA, "2026-10");

        Assert.Equal(
            ["2025-11", "2025-12", "2026-01", "2026-02", "2026-03", "2026-04",
             "2026-05", "2026-06", "2026-07", "2026-08", "2026-09", "2026-10"],
            months.Select(m => m.Month).Order());
    }

    // Weekly 70 → October 310.00 and September 300.00 (D-25).
    // Spent = budget is Green; one cent over is Red. Other users' data is excluded.
    [Fact]
    public async Task Months_GreenAtBudget_RedOneCentOver()
    {
        await AddAsync(
            new Budget(TestUsers.UserA, 70m),
            Expense(TestUsers.UserA, "2026-10-01", 300.00m),
            Expense(TestUsers.UserA, "2026-10-31", 10.00m),
            Expense(TestUsers.UserA, "2026-09-30", 300.01m),
            Expense(TestUsers.UserB, "2026-10-15", 500m),
            Fixed(TestUsers.UserB, "2026-01-01", FixedFrequency.Weekly, 50m),
            new Budget(TestUsers.UserB, 1m));

        var months = await GetMonthsAsync(TestUsers.UserA, "2026-10");

        var october = Month(months, "2026-10");
        Assert.Equal(310.00m, october.MonthlyBudget);
        Assert.Equal(310.00m, october.Spent);
        Assert.Equal(0m, october.Difference);
        Assert.Equal("Green", october.Status);

        var september = Month(months, "2026-09");
        Assert.Equal(300.00m, september.MonthlyBudget);
        Assert.Equal(300.01m, september.Spent);
        Assert.Equal(-0.01m, september.Difference);
        Assert.Equal("Red", september.Status);

        // A month with nothing spent is within the budget.
        var august = Month(months, "2026-08");
        Assert.Equal(0m, august.Spent);
        Assert.Equal("Green", august.Status);
    }

    // A monthly fixed expense counts once per month; a weekly one 4 or 5 times.
    // No payments before the start date.
    [Fact]
    public async Task Months_CountFixedPaymentsInEachMonth()
    {
        await AddAsync(
            Fixed(TestUsers.UserA, "2026-01-15", FixedFrequency.Monthly, 10m),
            // Thursday 1 Oct: 5 Thursdays in October 2026, 4 in November.
            Fixed(TestUsers.UserA, "2026-10-01", FixedFrequency.Weekly, 1m));

        var months = await GetMonthsAsync(TestUsers.UserA, "2026-11");

        Assert.Equal(0m, Month(months, "2025-12").Spent);
        Assert.Equal(10m, Month(months, "2026-01").Spent);
        Assert.Equal(10m, Month(months, "2026-09").Spent);
        Assert.Equal(15m, Month(months, "2026-10").Spent);
        Assert.Equal(14m, Month(months, "2026-11").Spent);
    }

    // Without a budget: spent only, no budget, difference or colour.
    [Fact]
    public async Task Months_WithoutBudget_ReturnsNullBudgetFigures()
    {
        await AddAsync(Expense(TestUsers.UserA, "2026-10-15", 42m));

        var months = await GetMonthsAsync(TestUsers.UserA, "2026-10");

        Assert.All(months, m =>
        {
            Assert.Null(m.MonthlyBudget);
            Assert.Null(m.Difference);
            Assert.Null(m.Status);
        });
        Assert.Equal(42m, Month(months, "2026-10").Spent);
    }

    [Theory]
    [InlineData("/api/summary/months")]
    [InlineData("/api/summary/months?endMonth=")]
    [InlineData("/api/summary/months?endMonth=abc")]
    [InlineData("/api/summary/months?endMonth=2026-13")]
    [InlineData("/api/summary/months?endMonth=2026-10-01")]
    public async Task Months_MissingOrInvalidEndMonth_Returns400(string path)
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<MonthResponse[]> GetMonthsAsync(Guid userId, string endMonth)
    {
        using var client = Factory.CreateClientFor(userId);
        var months = await client.GetFromJsonAsync<MonthResponse[]>($"/api/summary/months?endMonth={endMonth}");
        Assert.NotNull(months);
        Assert.Equal(12, months.Length);
        return months;
    }

    private static MonthResponse Month(MonthResponse[] months, string month) => months.Single(m => m.Month == month);

    private static Expense Expense(Guid userId, string date, decimal amount) =>
        new(userId, CategorySeed.Groceries, amount, "AUD", DateOnly.Parse(date), null, null);

    private static FixedExpense Fixed(Guid userId, string startDate, FixedFrequency frequency, decimal amount) =>
        new(userId, CategorySeed.Bills, "Fixed", amount, "AUD", DateOnly.Parse(startDate), frequency);

    private async Task AddAsync(params object[] entities)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }

    private sealed record MonthResponse(string Month, decimal? MonthlyBudget, decimal Spent, decimal? Difference, string? Status);
}
