using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/summary")]
public sealed class SummaryController(ISummaryService summary) : ControllerBase
{
    // The frontend sends its own "today", so the server never guesses the user's timezone.
    [HttpGet("week")]
    public Task<WeekSummaryDto> Week([FromQuery] DateOnly? date, CancellationToken cancellationToken) =>
        summary.GetWeekAsync(date, cancellationToken);

    // The frontend sends its own current month as endMonth (yyyy-MM).
    [HttpGet("months")]
    public Task<IReadOnlyList<MonthSummaryDto>> Months([FromQuery] string? endMonth, CancellationToken cancellationToken) =>
        summary.GetMonthsAsync(endMonth, cancellationToken);
}
