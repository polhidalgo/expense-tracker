using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.Infrastructure;

// Runs the real API in memory against the test database.
// The "Testing" environment means the development secrets are never loaded.
// By default the user comes from a test header; realAuthentication keeps the Supabase token validation.
public sealed class ApiFactory(
    string testConnectionString,
    string encryptionKey,
    string supabaseUrl,
    bool realAuthentication = false) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", testConnectionString);
        builder.UseSetting("Secrets:EncryptionKey", encryptionKey);
        builder.UseSetting("Supabase:Url", supabaseUrl);

        if (realAuthentication)
            return;

        builder.ConfigureTestServices(services =>
            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null));
    }

    public HttpClient CreateClientFor(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, userId.ToString());
        return client;
    }
}
