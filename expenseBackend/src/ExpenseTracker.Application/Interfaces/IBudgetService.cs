using ExpenseTracker.Application.DTOs;

namespace ExpenseTracker.Application.Interfaces;

public interface IBudgetService
{
    Task<BudgetDto> GetAsync(CancellationToken cancellationToken = default);

    Task<BudgetDto> SetAsync(BudgetRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(CancellationToken cancellationToken = default);
}
