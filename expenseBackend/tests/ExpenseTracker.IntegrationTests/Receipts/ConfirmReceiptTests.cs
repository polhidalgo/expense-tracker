using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.Receipts;

[Collection(DatabaseCollection.Name)]
public sealed class ConfirmReceiptTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    // One real Groq call. Only the user who analyzed can confirm, invalid values can be fixed and retried,
    // the edited values are saved as AiReceipt, and the same analysis cannot be confirmed twice.
    [Fact]
    [Trait("Category", "Groq")]
    public async Task Confirm_AfterAnalyze_SavesEditedValuesAsAiReceipt_OnlyOnce()
    {
        using var userA = Factory.CreateClientFor(TestUsers.UserA);
        using var userB = Factory.CreateClientFor(TestUsers.UserB);
        var analysisId = await AnalyzeAsync(userA);

        var otherUser = await userB.PostAsJsonAsync("/api/receipts/confirm", Confirmation(analysisId, 9.50m));
        Assert.Equal(HttpStatusCode.BadRequest, otherUser.StatusCode);

        var invalidAmount = await userA.PostAsJsonAsync("/api/receipts/confirm", Confirmation(analysisId, 0m));
        Assert.Equal(HttpStatusCode.BadRequest, invalidAmount.StatusCode);

        var saved = await userA.PostAsJsonAsync("/api/receipts/confirm", Confirmation(analysisId, 9.50m));
        Assert.Equal(HttpStatusCode.Created, saved.StatusCode);
        Assert.NotNull(saved.Headers.Location);
        var expense = await saved.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.NotNull(expense);
        Assert.Equal("AiReceipt", expense.Source);
        Assert.Equal(9.50m, expense.Amount);
        Assert.Equal(TestUsers.UserA, expense.UserId);

        var again = await userA.PostAsJsonAsync("/api/receipts/confirm", Confirmation(analysisId, 9.50m));
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);

        Assert.Equal(1, await CountExpensesAsync());
    }

    // An analysisId that was never issued is rejected and nothing is saved.
    [Fact]
    public async Task Confirm_UnknownAnalysisId_Returns400_AndSavesNothing()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.PostAsJsonAsync("/api/receipts/confirm", Confirmation(Guid.NewGuid(), 9.50m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("analysisId"));
        Assert.Equal(0, await CountExpensesAsync());
    }

    private static object Confirmation(Guid analysisId, decimal amount) => new
    {
        analysisId,
        amount,
        currency = "AUD",
        expenseDate = "2026-10-12",
        categoryId = CategorySeed.Groceries,
        merchant = "ALDI",
        description = (string?)null
    };

    private static async Task<Guid> AnalyzeAsync(HttpClient client)
    {
        var path = Path.Combine("TestFiles", "receipt.png");
        Assert.True(File.Exists(path), "TestFiles/receipt.png is missing. It is kept out of git; add it locally.");

        var file = new ByteArrayContent(await File.ReadAllBytesAsync(path));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var response = await client.PostAsync("/api/receipts/analyze", new MultipartFormDataContent { { file, "file", "receipt.png" } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var analysis = await response.Content.ReadFromJsonAsync<AnalysisResponse>();
        return analysis!.AnalysisId;
    }

    private async Task<int> CountExpensesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Expenses.CountAsync();
    }

    private sealed record AnalysisResponse(Guid AnalysisId);

    private sealed record ExpenseResponse(Guid Id, Guid UserId, decimal Amount, string Source);
}
