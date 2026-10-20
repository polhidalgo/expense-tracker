namespace ExpenseTracker.Application.DTOs;

public sealed record DashboardQuery
{
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
}
