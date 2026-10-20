namespace ExpenseTracker.Infrastructure.Persistence;

public class AppSecret
{
    public string Name { get; set; } = string.Empty;
    public string EncryptedValue { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
