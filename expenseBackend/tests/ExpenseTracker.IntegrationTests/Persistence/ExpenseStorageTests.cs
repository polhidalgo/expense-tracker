using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ExpenseTracker.IntegrationTests.Persistence;

[Collection(DatabaseCollection.Name)]
public sealed class ExpenseStorageTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    // The fixed categories exist once; re-applying migrations adds no duplicates.
    [Fact]
    public async Task Categories_AreTheFixedList_AndAreNotDuplicated()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var names = await db.Categories.OrderBy(c => c.Id).Select(c => c.Name).ToListAsync();
        Assert.Equal(
            ["Groceries", "Dining", "Transport", "Housing", "Bills",
             "Health", "Shopping", "Entertainment", "Travel", "Other"],
            names);
    }

    // Amounts are stored exactly as entered.
    [Fact]
    public async Task Amount_IsSavedAndReadBackExactly()
    {
        var expense = new Expense(TestUsers.UserA, CategorySeed.Groceries, 42.50m, "AUD",
            new DateOnly(2026, 10, 18), "Woolworths", null, ExpenseSource.Manual);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var saved = await db.Expenses.SingleAsync(e => e.Id == expense.Id);
            var storedText = await db.Database
                .SqlQuery<string>($"SELECT \"Amount\"::text AS \"Value\" FROM \"Expenses\"")
                .SingleAsync();

            Assert.Equal(42.50m, saved.Amount);
            Assert.Equal("42.50", storedText);
        }
    }

    // The database itself rejects zero or negative amounts.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Database_RejectsNonPositiveAmount(int amount)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ex = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync($"""
            INSERT INTO "Expenses" ("Id", "UserId", "CategoryId", "Amount", "Currency", "ExpenseDate", "Source", "CreatedAt")
            VALUES ({Guid.NewGuid()}, {TestUsers.UserA}, {CategorySeed.Groceries}, {(decimal)amount}, 'AUD', DATE '2026-10-18', 'Manual', now())
            """));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal(0, await db.Expenses.CountAsync());
    }

    // An expense must use an existing category.
    [Fact]
    public async Task Database_RejectsUnknownCategory()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Expenses.Add(new Expense(TestUsers.UserA, Guid.NewGuid(), 10m, "AUD",
            new DateOnly(2026, 10, 18), null, null, ExpenseSource.Manual));

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ((PostgresException)ex.InnerException!).SqlState);
    }
}
