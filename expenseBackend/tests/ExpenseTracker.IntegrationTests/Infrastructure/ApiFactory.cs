using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpenseTracker.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API in memory, pointed at the test database.
/// The "Testing" environment means the Api's development user secrets
/// (and its development connection string) are never loaded.
/// </summary>
public sealed class ApiFactory(string testConnectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", testConnectionString);

        builder.ConfigureTestServices(services =>
        {
            services.AddHttpContextAccessor();
            services.RemoveAll<ICurrentUser>();
            services.AddScoped<ICurrentUser, TestCurrentUser>();
        });
    }

    /// <summary>Creates a client whose requests are made as <paramref name="userId"/>.</summary>
    public HttpClient CreateClientFor(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestCurrentUser.HeaderName, userId.ToString());
        return client;
    }
}
