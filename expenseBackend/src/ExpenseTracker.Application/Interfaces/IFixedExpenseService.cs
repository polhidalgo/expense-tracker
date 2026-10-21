using ExpenseTracker.Application.DTOs;

namespace ExpenseTracker.Application.Interfaces;

public interface IFixedExpenseService
{
    Task<IReadOnlyList<FixedExpenseDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<FixedExpenseDto> CreateAsync(FixedExpenseRequest request, CancellationToken cancellationToken = default);

    Task<FixedExpenseDto> UpdateAsync(Guid id, FixedExpenseRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
