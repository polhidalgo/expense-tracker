using ExpenseTracker.Domain.Rules;

namespace ExpenseTracker.Domain.Entities;

// One weekly budget per user, applied to every week and month.
public class Budget
{
    public Guid UserId { get; private set; }
    public decimal WeeklyAmount { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Used by EF, to read the entity from the database.
    private Budget()
    {
    }

    public Budget(Guid userId, decimal weeklyAmount)
    {
        UserId = userId;
        SetWeeklyAmount(weeklyAmount);
    }

    public void SetWeeklyAmount(decimal weeklyAmount)
    {
        // Same rules as an expense amount.
        string? error = null;

        if (weeklyAmount <= 0)
            error = "Weekly amount must be greater than zero.";
        else if (decimal.Round(weeklyAmount, 2) != weeklyAmount)
            error = "Weekly amount cannot have more than 2 decimal places.";
        else if (weeklyAmount > ExpenseRules.MaxAmount)
            error = "Weekly amount is too large.";

        if (error is not null)
            throw new DomainValidationException(new() { ["weeklyAmount"] = [error] });

        WeeklyAmount = weeklyAmount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Weekly ÷ 7 × days in the month, rounded to cents.
    public static decimal MonthlyAmount(decimal weeklyAmount, int year, int month) =>
        decimal.Round(weeklyAmount * DateTime.DaysInMonth(year, month) / 7, 2, MidpointRounding.AwayFromZero);
}
