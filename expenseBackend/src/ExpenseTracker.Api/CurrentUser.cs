using ExpenseTracker.Application.Interfaces;

namespace ExpenseTracker.Api;

// The signed-in user, from the "sub" claim of the validated Supabase token.
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value, out var userId)
            ? userId
            : throw new InvalidOperationException("There is no signed-in user.");
}
