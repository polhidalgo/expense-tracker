using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Receipts;
using ExpenseTracker.Infrastructure.Persistence;

namespace ExpenseTracker.UnitTests.Receipts;

public class ReceiptDraftCleanerTests
{
    private static readonly IReadOnlyList<CategoryDto> Categories =
        CategorySeed.All.Select(c => new CategoryDto(c.Id, c.Name)).ToList();

    // A correct answer is kept as it is.
    [Fact]
    public void Clean_ValidAnswer_KeepsValues()
    {
        var draft = ReceiptDraftCleaner.Clean(
            new RawReceipt("Test Market", 23.45m, "AUD", "2026-10-15", "Groceries"), Categories);

        Assert.Equal(new ReceiptDraft("Test Market", 23.45m, "AUD", new DateOnly(2026, 10, 15), CategorySeed.Groceries), draft);
    }

    // Invalid values become null, and the category name is matched ignoring case.
    [Fact]
    public void Clean_InvalidValues_BecomeNull()
    {
        var draft = ReceiptDraftCleaner.Clean(
            new RawReceipt("  ", -5m, "USD", "yesterday", "groceries"), Categories);

        Assert.Equal(new ReceiptDraft(null, null, null, null, CategorySeed.Groceries), draft);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("12.345")]
    public void Clean_AmountZeroOrTooManyDecimals_BecomesNull(string amount)
    {
        var draft = ReceiptDraftCleaner.Clean(
            new RawReceipt(null, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), null, null, null),
            Categories);

        Assert.Null(draft.Amount);
    }

    // A category that is not one of the fixed categories is left empty.
    [Fact]
    public void Clean_UnknownCategory_BecomesNull()
    {
        var draft = ReceiptDraftCleaner.Clean(new RawReceipt(null, null, null, null, "Pets"), Categories);

        Assert.Null(draft.CategoryId);
    }

    [Fact]
    public void Clean_LowercaseCurrency_BecomesAud()
    {
        var draft = ReceiptDraftCleaner.Clean(new RawReceipt(null, null, "aud", null, null), Categories);

        Assert.Equal("AUD", draft.Currency);
    }
}
