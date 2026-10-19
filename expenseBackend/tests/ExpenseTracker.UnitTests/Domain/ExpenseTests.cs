using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Rules;

namespace ExpenseTracker.UnitTests.Domain;

public class ExpenseTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Groceries = Guid.NewGuid();
    private static readonly Guid Transport = Guid.NewGuid();
    private static readonly DateOnly Yesterday = new(2026, 10, 18);

    private static Expense NewExpense(
        decimal amount = 42.50m,
        string? currency = "AUD",
        string? merchant = "Woolworths",
        string? description = "Weekly groceries",
        DateOnly? expenseDate = null,
        Guid? categoryId = null) =>
        new(Owner, categoryId ?? Groceries, amount, currency, expenseDate ?? Yesterday,
            merchant, description, ExpenseSource.Manual);

    // Asserts the action is rejected with an error for exactly this field.
    private static void AssertRejected(string field, Action action)
    {
        var ex = Assert.Throws<DomainValidationException>(action);
        Assert.Equal([field], ex.Errors.Keys);
    }

    // Valid input keeps its values and sets the owner/source/creation time.
    [Fact]
    public void Create_WithValidData_SavesValuesAndOwner()
    {
        var expense = NewExpense();

        Assert.NotEqual(Guid.Empty, expense.Id);
        Assert.Equal(Owner, expense.UserId);
        Assert.Equal(ExpenseSource.Manual, expense.Source);
        Assert.Equal(TimeSpan.Zero, expense.CreatedAt.Offset);
        Assert.Equal(42.50m, expense.Amount);
        Assert.Equal("AUD", expense.Currency);
        Assert.Equal(Yesterday, expense.ExpenseDate);
        Assert.Equal("Woolworths", expense.Merchant);
    }

    // Amount must be greater than zero.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithZeroOrNegativeAmount_IsRejected(int amount)
    {
        AssertRejected("amount", () => NewExpense(amount: amount));
    }

    [Fact]
    public void Create_WithOneCent_IsAccepted()
    {
        Assert.Equal(0.01m, NewExpense(amount: 0.01m).Amount);
    }

    // More than two decimals is rejected, never rounded.
    [Fact]
    public void Create_WithThreeDecimals_IsRejected()
    {
        AssertRejected("amount", () => NewExpense(amount: 42.505m));
    }

    // Amounts that do not fit numeric(18,2) are rejected.
    [Fact]
    public void Create_WithHugeAmount_IsRejected()
    {
        AssertRejected("amount", () => NewExpense(amount: 10_000_000_000_000_000m));
    }

    // Only uppercase AUD is supported.
    [Theory]
    [InlineData("USD")]
    [InlineData("aud")]
    [InlineData("AU")]
    [InlineData("")]
    public void Create_WithUnsupportedCurrency_IsRejected(string currency)
    {
        AssertRejected("currency", () => NewExpense(currency: currency));
    }

    // Currency defaults to AUD when omitted.
    [Fact]
    public void Create_WithoutCurrency_UsesAud()
    {
        Assert.Equal("AUD", NewExpense(currency: null).Currency);
    }

    // Whitespace-only text becomes null; other text is trimmed.
    [Fact]
    public void Create_CleansOptionalText()
    {
        var expense = NewExpense(merchant: "   ", description: "  Bread and milk ");

        Assert.Null(expense.Merchant);
        Assert.Equal("Bread and milk", expense.Description);
    }

    // Merchant max 150 and description max 500 characters.
    [Fact]
    public void Create_WithTextAtMaxLength_IsAccepted()
    {
        var expense = NewExpense(merchant: new string('m', 150), description: new string('d', 500));

        Assert.Equal(150, expense.Merchant!.Length);
    }

    [Fact]
    public void Create_WithTooLongMerchant_IsRejected()
    {
        AssertRejected("merchant", () => NewExpense(merchant: new string('m', 151)));
    }

    [Fact]
    public void Create_WithTooLongDescription_IsRejected()
    {
        AssertRejected("description", () => NewExpense(description: new string('d', 501)));
    }

    // Future dates are allowed.
    [Fact]
    public void Create_WithFutureDate_IsAccepted()
    {
        var nextYear = new DateOnly(2027, 10, 18);

        Assert.Equal(nextYear, NewExpense(expenseDate: nextYear).ExpenseDate);
    }

    // Every invalid field gets its own error.
    [Fact]
    public void Create_WithSeveralInvalidFields_ReportsEachOne()
    {
        var ex = Assert.Throws<DomainValidationException>(() =>
            NewExpense(amount: 0, currency: "USD", categoryId: Guid.Empty, expenseDate: default(DateOnly)));

        Assert.Equal(
            new[] { "amount", "categoryId", "currency", "expenseDate" },
            ex.Errors.Keys.Order());
    }

    // Update changes editable fields, never Id/owner/source/creation time.
    [Fact]
    public void Update_WithValidData_ChangesOnlyEditableFields()
    {
        var expense = NewExpense();
        var (id, owner, source, createdAt) = (expense.Id, expense.UserId, expense.Source, expense.CreatedAt);

        expense.Update(Transport, 7.20m, null, new DateOnly(2026, 10, 1), "Opal", null);

        Assert.Equal(Transport, expense.CategoryId);
        Assert.Equal(7.20m, expense.Amount);
        Assert.Equal("Opal", expense.Merchant);
        Assert.Equal(id, expense.Id);
        Assert.Equal(owner, expense.UserId);
        Assert.Equal(source, expense.Source);
        Assert.Equal(createdAt, expense.CreatedAt);
    }

    // Rejected update leaves the expense exactly as it was.
    [Fact]
    public void Update_WithInvalidAmount_ChangesNothing()
    {
        var expense = NewExpense();

        AssertRejected("amount", () =>
            expense.Update(Transport, -5m, "AUD", Yesterday, "Changed", null));

        Assert.Equal(Groceries, expense.CategoryId);
        Assert.Equal(42.50m, expense.Amount);
        Assert.Equal("Woolworths", expense.Merchant);
    }
}
