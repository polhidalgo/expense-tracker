namespace ExpenseTracker.Application.DTOs;

public sealed record ExpenseListQuery
{
    public const int MaxPageSize = 100;

    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public Guid? CategoryId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
