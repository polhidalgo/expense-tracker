using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpenseTracker.IntegrationTests.Infrastructure;

// Runs the real API in memory against the test database.
// The "Testing" environment means the development secrets are never loaded.
public sealed class ApiFactory(string testConnectionString, string encryptionKey) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", testConnectionString);
        builder.UseSetting("Secrets:EncryptionKey", encryptionKey);

        builder.ConfigureTestServices(services =>
        {
            services.AddHttpContextAccessor();
            services.RemoveAll<ICurrentUser>();
            services.AddScoped<ICurrentUser, TestCurrentUser>();
        });
    }

    public HttpClient CreateClientFor(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestCurrentUser.HeaderName, userId.ToString());
        return client;
    }
}
