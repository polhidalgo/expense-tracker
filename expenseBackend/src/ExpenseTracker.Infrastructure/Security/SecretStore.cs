using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Security;

public sealed class SecretStore(AppDbContext db, SecretProtector protector)
{
    public const string GroqApiKey = "Groq:ApiKey";

    public async Task<string?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var secret = await db.AppSecrets
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Name == name, cancellationToken);

        return secret is null ? null : protector.Unprotect(secret.EncryptedValue);
    }

    public async Task SetAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        var secret = await db.AppSecrets.FindAsync([name], cancellationToken);

        if (secret is null)
        {
            secret = new AppSecret { Name = name };
            db.AppSecrets.Add(secret);
        }

        secret.EncryptedValue = protector.Protect(value);
        secret.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }
}
