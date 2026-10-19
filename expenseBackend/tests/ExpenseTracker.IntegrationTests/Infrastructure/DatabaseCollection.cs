// All integration tests share one database, so they must never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ExpenseTracker.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
{
    public const string Name = "Database";
}

/// <summary>
/// Base for tests that touch the database: resets all tables before each test.
/// Derived classes must also carry <c>[Collection(DatabaseCollection.Name)]</c>.
/// </summary>
public abstract class IntegrationTestBase(TestDatabaseFixture database) : IAsyncLifetime
{
    protected ApiFactory Factory => database.Factory;

    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
