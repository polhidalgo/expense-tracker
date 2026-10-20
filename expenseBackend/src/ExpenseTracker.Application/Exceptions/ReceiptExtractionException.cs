namespace ExpenseTracker.Application.Exceptions;

public enum ReceiptFailure
{
    Unavailable,
    RateLimited,
    InvalidResponse
}

public sealed class ReceiptExtractionException(ReceiptFailure reason)
    : Exception($"Receipt extraction failed: {reason}.")
{
    public ReceiptFailure Reason { get; } = reason;
}
