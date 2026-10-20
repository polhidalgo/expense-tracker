using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Application.Receipts;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Rules;
using ExpenseTracker.Infrastructure.AI;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Infrastructure.Services;

// Analyzing a receipt never saves an expense. It only returns a draft for the user to review.
public sealed class ReceiptService(
    GroqReceiptExtractor extractor,
    IExpenseService expenses,
    IMemoryCache cache,
    ICurrentUser currentUser,
    ILogger<ReceiptService> logger) : IReceiptService
{
    private static readonly TimeSpan AnalysisLifetime = TimeSpan.FromMinutes(30);

    private static readonly IReadOnlyList<CategoryDto> Categories =
        CategorySeed.All.Select(c => new CategoryDto(c.Id, c.Name)).ToList();

    public async Task<ReceiptAnalysisDto> AnalyzeAsync(byte[] image, CancellationToken cancellationToken = default)
    {
        var contentType = ReceiptFileCheck.GetImageType(image);

        var raw = await extractor.ExtractAsync(image, contentType, cancellationToken);
        var draft = ReceiptDraftCleaner.Clean(raw, Categories);

        // Remembers who analyzed it, so only that user can confirm it later.
        var analysisId = Guid.NewGuid();
        cache.Set(AnalysisKey(analysisId), currentUser.UserId, AnalysisLifetime);

        logger.LogInformation("Receipt analyzed ({Size} bytes, {ContentType})", image.Length, contentType);

        return new ReceiptAnalysisDto(analysisId, draft);
    }

    public async Task<ExpenseDto> ConfirmAsync(ConfirmReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var key = AnalysisKey(request.AnalysisId);

        if (!cache.TryGetValue(key, out Guid ownerId) || ownerId != currentUser.UserId)
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["analysisId"] = ["This analysis is not valid or has expired. Analyze the receipt again."]
            });
        }

        var expense = await expenses.CreateAsync(request.ToExpenseRequest(), ExpenseSource.AiReceipt, cancellationToken);

        // Removed only after saving, so the user can fix invalid values and try again.
        cache.Remove(key);

        return expense;
    }

    private static string AnalysisKey(Guid analysisId) => $"receipt-analysis:{analysisId}";
}
