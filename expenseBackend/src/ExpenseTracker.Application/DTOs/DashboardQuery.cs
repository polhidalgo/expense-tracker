namespace ExpenseTracker.Application.DTOs;

// Both dates are required and inclusive.
public sealed record DashboardQuery
{
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
}
