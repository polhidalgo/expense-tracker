using System.Globalization;
using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Domain.Rules;

namespace ExpenseTracker.Application.Receipts;

// The AI answer is untrusted: any value that is not valid becomes null. Nothing is invented.
public static class ReceiptDraftCleaner
{
    public static ReceiptDraft Clean(RawReceipt raw, IReadOnlyList<CategoryDto> categories) => new(
        CleanMerchant(raw.Merchant),
        CleanAmount(raw.Amount),
        CleanCurrency(raw.Currency),
        CleanDate(raw.Date),
        categories.FirstOrDefault(c => string.Equals(c.Name, raw.Category?.Trim(), StringComparison.OrdinalIgnoreCase))?.Id);

    private static string? CleanMerchant(string? merchant)
    {
        var trimmed = merchant?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > ExpenseRules.MaxMerchantLength ? null : trimmed;
    }

    private static decimal? CleanAmount(decimal? amount) =>
        amount is > 0 and <= ExpenseRules.MaxAmount && decimal.Round(amount.Value, 2) == amount ? amount : null;

    private static string? CleanCurrency(string? currency)
    {
        var code = currency?.Trim().ToUpperInvariant();
        return code is not null && ExpenseRules.SupportedCurrencies.Contains(code) ? code : null;
    }

    private static DateOnly? CleanDate(string? date) =>
        DateOnly.TryParseExact(date?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
}
