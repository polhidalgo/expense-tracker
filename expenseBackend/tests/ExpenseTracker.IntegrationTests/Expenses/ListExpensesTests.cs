using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.Expenses;

[Collection(DatabaseCollection.Name)]
public sealed class ListExpensesTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    // AC-08 / FR-EXP-003: only the user's expenses in the range are returned, boundaries included.
    [Fact]
    public async Task List_FiltersByDateRangeAndCategory_ForCurrentUserOnly()
    {
        await AddExpensesAsync(
            Expense(TestUsers.UserA, "2026-09-30", CategorySeed.Groceries, "before range"),
            Expense(TestUsers.UserA, "2026-10-01", CategorySeed.Groceries, "start boundary"),
            Expense(TestUsers.UserA, "2026-10-15", CategorySeed.Dining, "middle dining"),
            Expense(TestUsers.UserA, "2026-10-31", CategorySeed.Groceries, "end boundary"),
            Expense(TestUsers.UserA, "2026-11-01", CategorySeed.Groceries, "after range"),
            Expense(TestUsers.UserB, "2026-10-15", CategorySeed.Groceries, "other user"));

        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var inRange = await client.GetFromJsonAsync<PageResponse>(
            "/api/expenses?startDate=2026-10-01&endDate=2026-10-31");

        Assert.NotNull(inRange);
        Assert.Equal(["end boundary", "middle dining", "start boundary"], inRange.Items.Select(i => i.Merchant));
        Assert.Equal(3, inRange.TotalCount);

        var groceries = await client.GetFromJsonAsync<PageResponse>(
            $"/api/expenses?startDate=2026-10-01&endDate=2026-10-31&categoryId={CategorySeed.Groceries}");

        Assert.NotNull(groceries);
        Assert.Equal(["end boundary", "start boundary"], groceries.Items.Select(i => i.Merchant));
        Assert.Equal(2, groceries.TotalCount);
    }

    // FR-EXP-003: an empty result is 200 with no items, not an error.
    [Fact]
    public async Task List_NoMatches_ReturnsEmptyPage()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.GetAsync("/api/expenses?startDate=2020-01-01&endDate=2020-01-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PageResponse>();
        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    // AC-09: with 25 expenses, page 2 of size 10 holds items 11–20, newest first.
    [Fact]
    public async Task List_SecondPage_ReturnsItems11To20_InOrder()
    {
        var firstDay = new DateOnly(2026, 1, 1);
        await AddExpensesAsync(Enumerable.Range(0, 25)
            .Select(i => Expense(TestUsers.UserA, firstDay.AddDays(i).ToString("yyyy-MM-dd"), CategorySeed.Other, $"day {i}"))
            .ToArray());

        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var page = await client.GetFromJsonAsync<PageResponse>("/api/expenses?page=2&pageSize=10");

        Assert.NotNull(page);
        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        // Newest first: item 1 is day 24, so items 11–20 are days 14 down to 5.
        Assert.Equal(
            Enumerable.Range(5, 10).Reverse().Select(d => firstDay.AddDays(d)),
            page.Items.Select(i => i.ExpenseDate));
    }

    // AC-10: invalid paging or date range → 400 with the field error.
    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("pageSize=0", "pageSize")]
    [InlineData("pageSize=101", "pageSize")]
    [InlineData("startDate=2026-10-31&endDate=2026-10-01", "startDate")]
    public async Task List_InvalidQuery_Returns400(string queryString, string field)
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.GetAsync($"/api/expenses?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(field), $"Expected an error for '{field}'.");
    }

    private static Expense Expense(Guid userId, string date, Guid categoryId, string merchant) =>
        new(userId, categoryId, 10m, "AUD", DateOnly.Parse(date), merchant, null, ExpenseSource.Manual);

    private async Task AddExpensesAsync(params Expense[] expenses)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Expenses.AddRange(expenses);
        await db.SaveChangesAsync();
    }

    private sealed record PageResponse(ItemResponse[] Items, int Page, int PageSize, int TotalCount);

    private sealed record ItemResponse(Guid Id, DateOnly ExpenseDate, string? Merchant);
}
