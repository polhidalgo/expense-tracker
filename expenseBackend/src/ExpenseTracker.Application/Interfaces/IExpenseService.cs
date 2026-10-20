using ExpenseTracker.Application.DTOs;

namespace ExpenseTracker.Application.Interfaces;

// All operations act on the current user's expenses only.
public interface IExpenseService
{
    Task<ExpenseDto> CreateAsync(ExpenseRequest request, CancellationToken cancellationToken = default);

    Task<ExpenseDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<ExpenseDto>> ListAsync(ExpenseListQuery query, CancellationToken cancellationToken = default);

    Task<ExpenseDto> UpdateAsync(Guid id, ExpenseRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
