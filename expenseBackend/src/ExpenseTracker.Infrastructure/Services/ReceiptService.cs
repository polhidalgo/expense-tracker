using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Application.Receipts;
using ExpenseTracker.Infrastructure.AI;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Infrastructure.Services;

// Analyzing a receipt never saves an expense. It only returns a draft for the user to review.
public sealed class ReceiptService(GroqReceiptExtractor extractor, ILogger<ReceiptService> logger) : IReceiptService
{
    private static readonly IReadOnlyList<CategoryDto> Categories =
        CategorySeed.All.Select(c => new CategoryDto(c.Id, c.Name)).ToList();

    public async Task<ReceiptDraft> AnalyzeAsync(byte[] image, CancellationToken cancellationToken = default)
    {
        var contentType = ReceiptFileCheck.GetImageType(image);

        var raw = await extractor.ExtractAsync(image, contentType, cancellationToken);

        logger.LogInformation("Receipt analyzed ({Size} bytes, {ContentType})", image.Length, contentType);

        return ReceiptDraftCleaner.Clean(raw, Categories);
    }
}
