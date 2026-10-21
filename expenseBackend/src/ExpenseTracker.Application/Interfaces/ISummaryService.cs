using ExpenseTracker.Application.DTOs;

namespace ExpenseTracker.Application.Interfaces;

public interface ISummaryService
{
    // The Monday–Sunday week that contains the date.
    Task<WeekSummaryDto> GetWeekAsync(DateOnly? date, CancellationToken cancellationToken = default);

    // The 12 months ending at endMonth ("yyyy-MM"), oldest first.
    Task<IReadOnlyList<MonthSummaryDto>> GetMonthsAsync(string? endMonth, CancellationToken cancellationToken = default);
}
