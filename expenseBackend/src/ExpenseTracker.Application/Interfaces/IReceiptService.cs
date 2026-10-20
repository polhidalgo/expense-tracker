using ExpenseTracker.Application.DTOs;

namespace ExpenseTracker.Application.Interfaces;

public interface IReceiptService
{
    Task<ReceiptAnalysisDto> AnalyzeAsync(byte[] image, CancellationToken cancellationToken = default);

    Task<ExpenseDto> ConfirmAsync(ConfirmReceiptRequest request, CancellationToken cancellationToken = default);
}
