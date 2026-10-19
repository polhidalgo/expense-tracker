using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Rules;

namespace ExpenseTracker.Domain.Entities;

public class Expense
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = ExpenseRules.DefaultCurrency;
    public DateOnly ExpenseDate { get; private set; }
    public string? Merchant { get; private set; }
    public string? Description { get; private set; }
    public ExpenseSource Source { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Used by EF 
    private Expense()
    {
    }

    public Expense(
        Guid userId,
        Guid categoryId,
        decimal amount,
        string? currency,
        DateOnly expenseDate,
        string? merchant,
        string? description,
        ExpenseSource source)
    {
        SetDetails(categoryId, amount, currency, expenseDate, merchant, description);

        Id = Guid.CreateVersion7();
        UserId = userId;
        Source = source;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(
        Guid categoryId,
        decimal amount,
        string? currency,
        DateOnly expenseDate,
        string? merchant,
        string? description)
    {
        SetDetails(categoryId, amount, currency, expenseDate, merchant, description);
    }

    private void SetDetails(
        Guid categoryId,
        decimal amount,
        string? currency,
        DateOnly expenseDate,
        string? merchant,
        string? description)
    {
        currency ??= ExpenseRules.DefaultCurrency;
        merchant = CleanText(merchant);
        description = CleanText(description);

        var errors = new Dictionary<string, string[]>();

        if (categoryId == Guid.Empty)
            errors["categoryId"] = ["Category is required."];

        if (amount <= 0)
            errors["amount"] = ["Amount must be greater than zero."];
        else if (decimal.Round(amount, 2) != amount)
            errors["amount"] = ["Amount cannot have more than 2 decimal places."];
        else if (amount > ExpenseRules.MaxAmount)
            errors["amount"] = ["Amount is too large."];

        if (!ExpenseRules.SupportedCurrencies.Contains(currency))
            errors["currency"] = [$"Currency must be one of: {string.Join(", ", ExpenseRules.SupportedCurrencies)}."];

        if (expenseDate == default)
            errors["expenseDate"] = ["Expense date is required."];

        if (merchant?.Length > ExpenseRules.MaxMerchantLength)
            errors["merchant"] = [$"Merchant must be at most {ExpenseRules.MaxMerchantLength} characters."];

        if (description?.Length > ExpenseRules.MaxDescriptionLength)
            errors["description"] = [$"Description must be at most {ExpenseRules.MaxDescriptionLength} characters."];

        if (errors.Count > 0)
            throw new DomainValidationException(errors);

        CategoryId = categoryId;
        Amount = amount;
        Currency = currency;
        ExpenseDate = expenseDate;
        Merchant = merchant;
        Description = description;
    }

    private static string? CleanText(string? text)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
