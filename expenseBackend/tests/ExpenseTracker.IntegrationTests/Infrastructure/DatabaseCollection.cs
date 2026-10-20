// All integration tests share one database, so they must never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ExpenseTracker.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
{
    public const string Name = "Database";
}

// Base class for database tests: empties the tables before each test.
public abstract class IntegrationTestBase(TestDatabaseFixture database) : IAsyncLifetime
{
    protected TestDatabaseFixture Database => database;

    protected ApiFactory Factory => database.Factory;

    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
