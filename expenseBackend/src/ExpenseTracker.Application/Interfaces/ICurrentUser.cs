namespace ExpenseTracker.Application.Interfaces;

/// <summary>
/// Identity of the caller. Record ownership is always derived from here,
/// never from values submitted in a request.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
}
