using ExpenseTracker.Application.Interfaces;

namespace ExpenseTracker.Api;

// Replaced by real authentication before any public deployment.
public sealed class LocalCurrentUser(IConfiguration configuration) : ICurrentUser
{
    public Guid UserId { get; } = Guid.TryParse(configuration["LocalUser:Id"], out var userId)
        ? userId
        : throw new InvalidOperationException(
            "Local user is not configured. Set it with " +
            "'dotnet user-secrets set \"LocalUser:Id\" \"<guid>\" --project src/ExpenseTracker.Api'.");
}
