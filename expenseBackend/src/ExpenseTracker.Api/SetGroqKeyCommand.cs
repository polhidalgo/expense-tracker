using ExpenseTracker.Infrastructure.Security;

namespace ExpenseTracker.Api;

// dotnet run --project src/ExpenseTracker.Api -- set-groq-key

public static class SetGroqKeyCommand
{
    public const string Name = "set-groq-key";

    public static async Task RunAsync(IServiceProvider services)
    {
        Console.Write("Groq API key: ");
        var key = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(key))
        {
            Console.WriteLine("No key entered. Nothing was saved.");
            return;
        }

        using var scope = services.CreateScope();
        var secrets = scope.ServiceProvider.GetRequiredService<SecretStore>();
        await secrets.SetAsync(SecretStore.GroqApiKey, key);

        Console.WriteLine("Groq API key saved (encrypted).");
    }
}
