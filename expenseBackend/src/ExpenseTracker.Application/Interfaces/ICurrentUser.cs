namespace ExpenseTracker.Application.Interfaces;

// The user making the request. Owners always come from here, never from the request body.
public interface ICurrentUser
{
    Guid UserId { get; }
}
