using System.Globalization;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Rules;

namespace ExpenseTracker.UnitTests.Domain;

public class BudgetTests
{
    [Fact]
    public void Create_WithValidAmount_SavesValues()
    {
        var owner = Guid.NewGuid();

        var budget = new Budget(owner, 150.50m);

        Assert.Equal(owner, budget.UserId);
        Assert.Equal(150.50m, budget.WeeklyAmount);
    }

    // Same amount rules as an expense: > 0, at most 2 decimals, fits numeric(18,2).
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("12.345")]
    [InlineData("10000000000000000")]
    public void Create_WithInvalidAmount_IsRejected(string amount)
    {
        var ex = Assert.Throws<DomainValidationException>(() => new Budget(Guid.NewGuid(), decimal.Parse(amount, CultureInfo.InvariantCulture)));

        Assert.Equal(["weeklyAmount"], ex.Errors.Keys);
    }

    [Fact]
    public void SetWeeklyAmount_WithInvalidAmount_KeepsOldAmount()
    {
        var budget = new Budget(Guid.NewGuid(), 100m);

        Assert.Throws<DomainValidationException>(() => budget.SetWeeklyAmount(0m));

        Assert.Equal(100m, budget.WeeklyAmount);
    }

    // Weekly budget ÷ 7 multiplied by days in the month, should return expected monthly amount
    [Theory]
    [InlineData("70", 2026, 10, "310.00")]
    [InlineData("70", 2026, 2, "280.00")]
    [InlineData("100", 2026, 4, "428.57")]
    public void MonthlyAmount_IsWeeklyPerDayTimesDaysInMonth(string weekly, int year, int month, string expected)
    {
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), Budget.MonthlyAmount(decimal.Parse(weekly, CultureInfo.InvariantCulture), year, month));
    }
}
