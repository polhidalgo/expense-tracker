using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.Summary;

// The week used here is Monday 12 Oct 2026 to Sunday 18 Oct 2026.
[Collection(DatabaseCollection.Name)]
public sealed class WeekSummaryTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    private static readonly DateOnly Monday = new(2026, 10, 12);
    private static readonly DateOnly Sunday = new(2026, 10, 18);

    // Spent = expenses Monday–Sunday + fixed payments due that week; other users' data excluded.
    [Theory]
    [InlineData("2026-10-12")]
    [InlineData("2026-10-14")]
    [InlineData("2026-10-18")]
    public async Task Week_TotalsExpensesAndFixedPayments_ForAnyDayOfTheWeek(string date)
    {
        await AddAsync(
            new Budget(TestUsers.UserA, 100m),
            Expense(TestUsers.UserA, "2026-10-12", CategorySeed.Groceries, 20.00m),
            Expense(TestUsers.UserA, "2026-10-18", CategorySeed.Dining, 5.50m),
            Expense(TestUsers.UserA, "2026-10-11", CategorySeed.Groceries, 100m),
            Expense(TestUsers.UserA, "2026-10-19", CategorySeed.Groceries, 200m),
            Expense(TestUsers.UserB, "2026-10-14", CategorySeed.Groceries, 300m),
            // Weekly from Thu 1 Oct: due 15 Oct in this week.
            Fixed(TestUsers.UserA, "Rent", CategorySeed.Housing, 30.00m, "2026-10-01", FixedFrequency.Weekly),
            // Monthly from 14 Sep: due 14 Oct in this week.
            Fixed(TestUsers.UserA, "Phone", CategorySeed.Groceries, 10.00m, "2026-09-14", FixedFrequency.Monthly),
            // Starts after this week: no payment.
            Fixed(TestUsers.UserA, "Gym", CategorySeed.Health, 40m, "2026-10-19", FixedFrequency.Weekly),
            Fixed(TestUsers.UserB, "Other user", CategorySeed.Housing, 50m, "2026-10-12", FixedFrequency.Weekly));

        var week = await GetWeekAsync(TestUsers.UserA, date);

        Assert.Equal(Monday, week.WeekStart);
        Assert.Equal(Sunday, week.WeekEnd);
        Assert.Equal(100m, week.WeeklyBudget);
        Assert.Equal(65.50m, week.Spent);
        Assert.Equal(34.50m, week.Remaining);
        Assert.Equal(34.5m, week.PercentLeft);

        Assert.Equal(3, week.ByCategory.Length);
        Assert.Equal(30.00m, Total(week, CategorySeed.Groceries));
        Assert.Equal(30.00m, Total(week, CategorySeed.Housing));
        Assert.Equal(5.50m, Total(week, CategorySeed.Dining));

        Assert.Equal(
            [("Phone", new DateOnly(2026, 10, 14), 10.00m), ("Rent", new DateOnly(2026, 10, 15), 30.00m)],
            week.FixedPayments.Select(p => (p.Name, p.Date, p.Amount)).OrderBy(p => p.Date));
    }

    // Over budget: remaining is negative and % left is 0.
    [Fact]
    public async Task Week_OverBudget_RemainingNegative_PercentLeftZero()
    {
        await AddAsync(
            new Budget(TestUsers.UserA, 10m),
            Expense(TestUsers.UserA, "2026-10-14", CategorySeed.Dining, 25m));

        var week = await GetWeekAsync(TestUsers.UserA, "2026-10-14");

        Assert.Equal(25m, week.Spent);
        Assert.Equal(-15m, week.Remaining);
        Assert.Equal(0m, week.PercentLeft);
    }

    // Without a budget the budget figures are null; spent is still correct.
    [Fact]
    public async Task Week_WithoutBudget_ReturnsNullBudgetFigures()
    {
        await AddAsync(
            new Budget(TestUsers.UserB, 100m),
            Expense(TestUsers.UserA, "2026-10-14", CategorySeed.Dining, 12.30m));

        var week = await GetWeekAsync(TestUsers.UserA, "2026-10-14");

        Assert.Null(week.WeeklyBudget);
        Assert.Null(week.Remaining);
        Assert.Null(week.PercentLeft);
        Assert.Equal(12.30m, week.Spent);
    }

    [Fact]
    public async Task Week_Empty_ReturnsZeroSpent()
    {
        var week = await GetWeekAsync(TestUsers.UserA, "2026-10-14");

        Assert.Equal(0m, week.Spent);
        Assert.Empty(week.ByCategory);
        Assert.Empty(week.FixedPayments);
    }

    [Theory]
    [InlineData("/api/summary/week")]
    [InlineData("/api/summary/week?date=")]
    [InlineData("/api/summary/week?date=not-a-date")]
    [InlineData("/api/summary/week?date=2026-13-01")]
    public async Task Week_MissingOrInvalidDate_Returns400(string path)
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<WeekResponse> GetWeekAsync(Guid userId, string date)
    {
        using var client = Factory.CreateClientFor(userId);
        var week = await client.GetFromJsonAsync<WeekResponse>($"/api/summary/week?date={date}");
        Assert.NotNull(week);
        return week;
    }

    private static decimal Total(WeekResponse week, Guid categoryId) =>
        week.ByCategory.Single(c => c.CategoryId == categoryId).Total;

    private static Expense Expense(Guid userId, string date, Guid categoryId, decimal amount) =>
        new(userId, categoryId, amount, "AUD", DateOnly.Parse(date), null, null);

    private static FixedExpense Fixed(Guid userId, string name, Guid categoryId, decimal amount, string startDate, FixedFrequency frequency) =>
        new(userId, categoryId, name, amount, "AUD", DateOnly.Parse(startDate), frequency);

    private async Task AddAsync(params object[] entities)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }

    private sealed record WeekResponse(
        DateOnly WeekStart,
        DateOnly WeekEnd,
        decimal? WeeklyBudget,
        decimal Spent,
        decimal? Remaining,
        decimal? PercentLeft,
        CategoryResponse[] ByCategory,
        FixedPaymentResponse[] FixedPayments);

    private sealed record CategoryResponse(Guid CategoryId, decimal Total);

    private sealed record FixedPaymentResponse(string Name, DateOnly Date, decimal Amount);
}
