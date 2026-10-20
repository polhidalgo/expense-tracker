using ExpenseTracker.Application.DTOs;

namespace ExpenseTracker.Application.Interfaces;

public interface IReceiptService
{
    Task<ReceiptDraft> AnalyzeAsync(byte[] image, CancellationToken cancellationToken = default);
}
