using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Rules;

namespace ExpenseTracker.Domain.Entities;

// A template: its payments are calculated, never saved as expenses (D-23).
public class FixedExpense
{
    public const int MaxNameLength = 150;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = ExpenseRules.DefaultCurrency;
    public DateOnly StartDate { get; private set; }
    public FixedFrequency Frequency { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Used by EF
    private FixedExpense()
    {
    }

    public FixedExpense(
        Guid userId,
        Guid categoryId,
        string? name,
        decimal amount,
        string? currency,
        DateOnly startDate,
        FixedFrequency? frequency)
    {
        SetDetails(categoryId, name, amount, currency, startDate, frequency);

        Id = Guid.CreateVersion7();
        UserId = userId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(
        Guid categoryId,
        string? name,
        decimal amount,
        string? currency,
        DateOnly startDate,
        FixedFrequency? frequency)
    {
        SetDetails(categoryId, name, amount, currency, startDate, frequency);
    }

    // Due dates between from and to (both included): the start date, then every 7 or 14 days,
    // or the same day each month (the month's last day when that day does not exist).
    public IEnumerable<DateOnly> PaymentsBetween(DateOnly from, DateOnly to)
    {
        if (from < StartDate)
            from = StartDate;

        if (Frequency == FixedFrequency.Monthly)
        {
            // Always counted from the start date, so the 31st comes back after a short month.
            var monthsToFrom = (from.Year - StartDate.Year) * 12 + from.Month - StartDate.Month;
            for (var i = monthsToFrom; ; i++)
            {
                var date = StartDate.AddMonths(i);
                if (date > to)
                    yield break;
                if (date >= from)
                    yield return date;
            }
        }

        var step = Frequency == FixedFrequency.Weekly ? 7 : 14;
        var daysToFrom = from.DayNumber - StartDate.DayNumber;
        var first = StartDate.AddDays((daysToFrom + step - 1) / step * step);

        for (var date = first; date <= to; date = date.AddDays(step))
            yield return date;
    }

    private void SetDetails(
        Guid categoryId,
        string? name,
        decimal amount,
        string? currency,
        DateOnly startDate,
        FixedFrequency? frequency)
    {
        currency ??= ExpenseRules.DefaultCurrency;
        name = name?.Trim();

        var errors = new Dictionary<string, string[]>();

        if (categoryId == Guid.Empty)
            errors["categoryId"] = ["Category is required."];

        if (string.IsNullOrEmpty(name))
            errors["name"] = ["Name is required."];
        else if (name.Length > MaxNameLength)
            errors["name"] = [$"Name must be at most {MaxNameLength} characters."];

        if (amount <= 0)
            errors["amount"] = ["Amount must be greater than zero."];
        else if (decimal.Round(amount, 2) != amount)
            errors["amount"] = ["Amount cannot have more than 2 decimal places."];
        else if (amount > ExpenseRules.MaxAmount)
            errors["amount"] = ["Amount is too large."];

        if (!ExpenseRules.SupportedCurrencies.Contains(currency))
            errors["currency"] = [$"Currency must be one of: {string.Join(", ", ExpenseRules.SupportedCurrencies)}."];

        if (startDate == default)
            errors["startDate"] = ["Start date is required."];

        if (frequency is null || !Enum.IsDefined(frequency.Value))
            errors["frequency"] = ["Frequency must be Weekly, Fortnightly or Monthly."];

        if (errors.Count > 0)
            throw new DomainValidationException(errors);

        CategoryId = categoryId;
        Name = name!;
        Amount = amount;
        Currency = currency;
        StartDate = startDate;
        Frequency = frequency!.Value;
    }
}
