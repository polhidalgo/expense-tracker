using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ExpenseTracker.IntegrationTests.Infrastructure;

public sealed class TestDatabaseFixture : IAsyncLifetime
{
    // Must match <UserSecretsId> in the test project file.
    private const string UserSecretsId = "ae54879e-1304-4afa-91c4-368674e9db07";

    // Created by hand in the test database only. Without it the tests refuse to run.
    private const string MarkerTable = "__integration_test_marker";
    private const string MigrationsHistoryTable = "__EFMigrationsHistory";

    // The categories and the encrypted secrets (Groq key) are not deleted between tests.
    private const string CategoriesTable = "Categories";
    private const string AppSecretsTable = "AppSecrets";

    private string _connectionString = string.Empty;

    public ApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(UserSecretsId)
            .AddEnvironmentVariables()
            .Build();

        _connectionString = ReadConnectionString(configuration);
        var encryptionKey = ReadEncryptionKey(configuration);

        await EnsureIsTestDatabaseAsync(_connectionString);

        Factory = new ApiFactory(_connectionString, encryptionKey);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        EnsureApiUsesTestDatabase(db);

        await db.Database.MigrateAsync();
    }

    // Empties the tables before each test (except the ones above).
    public async Task ResetAsync()
    {
        const string sql = $"""
            DO $$
            DECLARE tables text;
            BEGIN
                SELECT string_agg(format('public.%I', tablename), ', ')
                INTO tables
                FROM pg_tables
                WHERE schemaname = 'public'
                  AND tablename NOT IN ('{MigrationsHistoryTable}', '{MarkerTable}', '{CategoriesTable}', '{AppSecretsTable}');

                IF tables IS NOT NULL THEN
                    EXECUTE 'TRUNCATE TABLE ' || tables || ' RESTART IDENTITY CASCADE';
                END IF;
            END $$;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }
    }

    private static string ReadEncryptionKey(IConfiguration configuration)
    {
        var encryptionKey = configuration["Secrets:EncryptionKey"];

        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            throw new InvalidOperationException(
                "Encryption key is not configured. Set 'Secrets:EncryptionKey' with " +
                "'dotnet user-secrets set \"Secrets:EncryptionKey\" \"<base64>\" " +
                "--project tests/ExpenseTracker.IntegrationTests' or the environment variable " +
                "'Secrets__EncryptionKey'. It must be the key used when the Groq key was saved in the test database.");
        }

        return encryptionKey;
    }

    private static string ReadConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TestDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Integration test database is not configured. Set 'ConnectionStrings:TestDatabase' with " +
                "'dotnet user-secrets set \"ConnectionStrings:TestDatabase\" \"<connection string>\" " +
                "--project tests/ExpenseTracker.IntegrationTests' or the environment variable " +
                "'ConnectionStrings__TestDatabase'.");
        }

        return connectionString;
    }

    private static async Task EnsureIsTestDatabaseAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            $"SELECT to_regclass('public.{MarkerTable}') IS NOT NULL", connection);

        var isTestDatabase = (bool)(await command.ExecuteScalarAsync())!;

        if (!isTestDatabase)
        {
            throw new InvalidOperationException(
                $"Refusing to run integration tests: table 'public.{MarkerTable}' was not found in " +
                $"database '{connection.Database}' on host '{connection.Host}'. Integration tests truncate " +
                "every table, so they only run against a database explicitly marked as the test database. " +
                $"If this IS the dedicated test database, run once in its SQL editor: CREATE TABLE public.{MarkerTable} ();");
        }
    }

    // Makes sure the API is really using the test database.
    private void EnsureApiUsesTestDatabase(AppDbContext db)
    {
        var expected = new NpgsqlConnectionStringBuilder(_connectionString);
        var actual = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());

        if (actual.Host != expected.Host
            || actual.Port != expected.Port
            || actual.Database != expected.Database
            || actual.Username != expected.Username)
        {
            throw new InvalidOperationException(
                "The API under test is not using the configured test database connection.");
        }
    }
}
