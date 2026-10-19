using ExpenseTracker.Application.DTOs;

namespace ExpenseTracker.Application.Interfaces;

public interface ICategoryService
{
    // The fixed categories, in their defined order (FR-CAT-001).
    Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken = default);
}
