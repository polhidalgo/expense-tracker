namespace ExpenseTracker.Application.Exceptions;

// Thrown when something does not exist or belongs to another user. Both look the same to the caller.
public sealed class NotFoundException(string message) : Exception(message);
