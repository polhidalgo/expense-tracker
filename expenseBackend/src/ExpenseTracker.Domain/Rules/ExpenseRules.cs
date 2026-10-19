namespace ExpenseTracker.Domain.Rules;

/// <summary>
/// Limits for expense fields (FR-EXP-001, D-02, D-03).
/// </summary>
public static class ExpenseRules
{
    // Largest value that fits PostgreSQL numeric(18,2).
    public const decimal MaxAmount = 9_999_999_999_999_999.99m;

    public const string DefaultCurrency = "AUD";

    // Only AUD for now. Add codes here to support more currencies.
    public static readonly string[] SupportedCurrencies = ["AUD"];

    public const int MaxMerchantLength = 150;
    public const int MaxDescriptionLength = 500;
}
