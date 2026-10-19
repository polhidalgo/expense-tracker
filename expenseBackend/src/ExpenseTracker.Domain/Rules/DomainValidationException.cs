namespace ExpenseTracker.Domain.Rules;

public class DomainValidationException : Exception
{
    public DomainValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public Dictionary<string, string[]> Errors { get; }
}
