namespace ExpenseTracker.Application.Exceptions;

public sealed class FileTooLargeException(string message) : Exception(message);
