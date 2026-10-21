using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Rules;

namespace ExpenseTracker.UnitTests.Domain;

public class FixedExpenseTests
{
    private static FixedExpense NewFixed(
        DateOnly startDate,
        FixedFrequency? frequency = FixedFrequency.Weekly,
        string? name = "Rent",
        decimal amount = 500m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, amount, "AUD", startDate, frequency);

    private static DateOnly D(int year, int month, int day) => new(year, month, day);

    [Fact]
    public void Create_WithValidData_SavesValues()
    {
        var fixedExpense = NewFixed(D(2026, 10, 7), FixedFrequency.Monthly);

        Assert.Equal("Rent", fixedExpense.Name);
        Assert.Equal(500m, fixedExpense.Amount);
        Assert.Equal("AUD", fixedExpense.Currency);
        Assert.Equal(FixedFrequency.Monthly, fixedExpense.Frequency);
    }

    // 7 Oct 2026 is a Wednesday.
    [Fact]
    public void PaymentsBetween_WeeklyFromWednesday_OverThreeWeeks_ReturnsThreeDates()
    {
        var fixedExpense = NewFixed(D(2026, 10, 7), FixedFrequency.Weekly);

        var payments = fixedExpense.PaymentsBetween(D(2026, 10, 5), D(2026, 10, 25));

        Assert.Equal([D(2026, 10, 7), D(2026, 10, 14), D(2026, 10, 21)], payments);
    }

    [Fact]
    public void PaymentsBetween_Fortnightly_ReturnsEvery14Days()
    {
        var fixedExpense = NewFixed(D(2026, 10, 7), FixedFrequency.Fortnightly);

        var payments = fixedExpense.PaymentsBetween(D(2026, 10, 10), D(2026, 11, 30));

        Assert.Equal([D(2026, 10, 21), D(2026, 11, 4), D(2026, 11, 18)], payments);
    }

    // The 31st falls back to the month's last day and comes back in longer months.
    [Fact]
    public void PaymentsBetween_MonthlyFrom31st_UsesLastDayOfShortMonths()
    {
        var fixedExpense = NewFixed(D(2026, 1, 31), FixedFrequency.Monthly);

        var payments = fixedExpense.PaymentsBetween(D(2026, 1, 1), D(2026, 4, 30));

        Assert.Equal([D(2026, 1, 31), D(2026, 2, 28), D(2026, 3, 31), D(2026, 4, 30)], payments);
    }

    [Fact]
    public void PaymentsBetween_MonthlyFrom31st_InLeapYear_UsesFeb29()
    {
        var fixedExpense = NewFixed(D(2028, 1, 31), FixedFrequency.Monthly);

        var payments = fixedExpense.PaymentsBetween(D(2028, 2, 1), D(2028, 2, 29));

        Assert.Equal([D(2028, 2, 29)], payments);
    }

    [Theory]
    [InlineData(FixedFrequency.Weekly)]
    [InlineData(FixedFrequency.Fortnightly)]
    [InlineData(FixedFrequency.Monthly)]
    public void PaymentsBetween_RangeBeforeStartDate_ReturnsNone(FixedFrequency frequency)
    {
        var fixedExpense = NewFixed(D(2026, 10, 7), frequency);

        Assert.Empty(fixedExpense.PaymentsBetween(D(2026, 9, 1), D(2026, 10, 6)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutName_IsRejected(string? name)
    {
        AssertRejected("name", () => NewFixed(D(2026, 10, 7), name: name));
    }

    // Name is at most 150 characters.
    [Fact]
    public void Create_NameOf150_IsAccepted_And151_IsRejected()
    {
        Assert.Equal(150, NewFixed(D(2026, 10, 7), name: new string('x', 150)).Name.Length);
        AssertRejected("name", () => NewFixed(D(2026, 10, 7), name: new string('x', 151)));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("12.345")]
    public void Create_WithInvalidAmount_IsRejected(string amount)
    {
        AssertRejected("amount", () => NewFixed(D(2026, 10, 7), amount: decimal.Parse(amount)));
    }

    [Fact]
    public void Create_WithUnknownOrMissingFrequency_IsRejected()
    {
        AssertRejected("frequency", () => NewFixed(D(2026, 10, 7), (FixedFrequency)99));
        AssertRejected("frequency", () => NewFixed(D(2026, 10, 7), null));
    }

    private static void AssertRejected(string field, Action action)
    {
        var ex = Assert.Throws<DomainValidationException>(action);
        Assert.Equal([field], ex.Errors.Keys);
    }
}
