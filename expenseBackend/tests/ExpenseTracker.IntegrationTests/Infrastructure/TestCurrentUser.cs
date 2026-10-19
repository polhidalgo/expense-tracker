using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ExpenseTracker.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only identity: the caller is whichever user the request's
/// <see cref="HeaderName"/> header names. Never registered outside tests.
/// </summary>
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
