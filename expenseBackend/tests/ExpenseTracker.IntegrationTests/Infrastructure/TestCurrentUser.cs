using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ExpenseTracker.IntegrationTests.Infrastructure;

// In tests, the user comes from the X-Test-User request header.
public sealed class TestCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public const string HeaderName = "X-Test-User";

    public Guid UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString();

            return Guid.TryParse(value, out var userId)
                ? userId
                : throw new InvalidOperationException(
                    $"Request is missing a valid '{HeaderName}' header. " +
                    "Create the client with ApiFactory.CreateClientFor(userId).");
        }
    }
}
