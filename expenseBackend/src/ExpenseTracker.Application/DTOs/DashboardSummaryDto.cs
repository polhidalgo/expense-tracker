namespace ExpenseTracker.Application.DTOs;

public sealed record DashboardSummaryDto(
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<CurrencyTotalDto> Totals,
    IReadOnlyList<CategoryTotalDto> ByCategory,
    IReadOnlyList<ExpenseDto> Recent);

public sealed record CurrencyTotalDto(string Currency, decimal Total, int Count);

public sealed record CategoryTotalDto(Guid CategoryId, string CategoryName, string Currency, decimal Total);
