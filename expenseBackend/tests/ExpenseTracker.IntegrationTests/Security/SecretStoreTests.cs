using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.Infrastructure.Security;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.Security;

[Collection(DatabaseCollection.Name)]
public sealed class SecretStoreTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    // The Groq key is in the test DB, stored encrypted, and readable with the test encryption key.
    // The per-test reset has already run, so this also shows that the reset keeps AppSecrets.
    // Only booleans are asserted so the key is never printed.
    [Fact]
    public async Task GroqKey_IsStoredEncrypted_AndCanBeRead()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var secrets = scope.ServiceProvider.GetRequiredService<SecretStore>();

        var row = await db.AppSecrets.AsNoTracking().SingleOrDefaultAsync(s => s.Name == SecretStore.GroqApiKey);
        Assert.True(row is not null,
            "The Groq key is not in the test database. Run 'set-groq-key' against the test database.");

        var key = await secrets.GetAsync(SecretStore.GroqApiKey);

        Assert.False(string.IsNullOrWhiteSpace(key));
        Assert.False(row.EncryptedValue.Contains(key!), "The stored value must not contain the key.");
    }

    // Setting a secret twice replaces it.
    [Fact]
    public async Task Set_TwiceWithSameName_ReplacesValue()
    {
        const string name = "Test:Example";

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var secrets = scope.ServiceProvider.GetRequiredService<SecretStore>();

        try
        {
            await secrets.SetAsync(name, "first");
            await secrets.SetAsync(name, "second");

            Assert.Equal("second", await secrets.GetAsync(name));
            Assert.Equal(1, await db.AppSecrets.CountAsync(s => s.Name == name));
        }
        finally
        {
            // The reset never empties AppSecrets, so this test cleans up after itself.
            await db.AppSecrets.Where(s => s.Name == name).ExecuteDeleteAsync();
        }
    }
}
