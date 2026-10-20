using ExpenseTracker.Application.DTOs;

namespace ExpenseTracker.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken = default);
}
