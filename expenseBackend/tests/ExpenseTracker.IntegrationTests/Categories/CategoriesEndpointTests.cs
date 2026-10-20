using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.IntegrationTests.Infrastructure;

namespace ExpenseTracker.IntegrationTests.Categories;

[Collection(DatabaseCollection.Name)]
public sealed class CategoriesEndpointTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    // Every user gets exactly the fixed list, in its defined order.
    [Fact]
    public async Task List_ReturnsTheFixedCategoriesInOrder_ForAnyUser()
    {
        CategoryResponse[] expected =
        [
            new(Guid.Parse("00000000-0000-0000-0000-000000000101"), "Groceries"),
            new(Guid.Parse("00000000-0000-0000-0000-000000000102"), "Dining"),
            new(Guid.Parse("00000000-0000-0000-0000-000000000103"), "Transport"),
            new(Guid.Parse("00000000-0000-0000-0000-000000000104"), "Housing"),
            new(Guid.Parse("00000000-0000-0000-0000-000000000105"), "Bills"),
            new(Guid.Parse("00000000-0000-0000-0000-000000000106"), "Health"),
            new(Guid.Parse("00000000-0000-0000-0000-000000000107"), "Shopping"),
            new(Guid.Parse("00000000-0000-0000-0000-000000000108"), "Entertainment"),
            new(Guid.Parse("00000000-0000-0000-0000-000000000109"), "Travel"),
            new(Guid.Parse("00000000-0000-0000-0000-000000000110"), "Other"),
        ];

        foreach (var user in new[] { TestUsers.UserA, TestUsers.UserB })
        {
            using var client = Factory.CreateClientFor(user);

            var response = await client.GetAsync("/api/categories");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var categories = await response.Content.ReadFromJsonAsync<CategoryResponse[]>();
            Assert.Equal(expected, categories);
        }
    }

    private sealed record CategoryResponse(Guid Id, string Name);
}
