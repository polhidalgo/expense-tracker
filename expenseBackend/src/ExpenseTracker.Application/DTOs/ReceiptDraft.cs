namespace ExpenseTracker.Application.DTOs;

// What the AI read from the image, before it is checked.
public sealed record RawReceipt(string? Merchant, decimal? Amount, string? Currency, string? Date, string? Category);

// Suggested values for a new expense. Nothing here is saved.
public sealed record ReceiptDraft(
    string? Merchant,
    decimal? Amount,
    string? Currency,
    DateOnly? ExpenseDate,
    Guid? CategoryId);

public sealed record ReceiptAnalysisDto(Guid AnalysisId, ReceiptDraft Draft);
