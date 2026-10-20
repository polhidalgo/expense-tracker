using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.IntegrationTests.Receipts;

[Collection(DatabaseCollection.Name)]
public sealed class AnalyzeReceiptTests(TestDatabaseFixture database) : IntegrationTestBase(database)
{
    // Screenshot (ALDI, -$8.76, 12 Oct 2026).
    [Fact]
    [Trait("Category", "Groq")]
    public async Task Analyze_PaymentScreenshot_ReturnsDraft_AndSavesNothing()
    {
        var path = Path.Combine("TestFiles", "receipt.png");
        Assert.True(File.Exists(path), "TestFiles/receipt.png is missing. It is kept out of git; add it locally.");

        using var client = Factory.CreateClientFor(TestUsers.UserA);
        var image = await File.ReadAllBytesAsync(path);

        var response = await client.PostAsync("/api/receipts/analyze", ImageForm(image, "receipt.png", "image/png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var analysis = await response.Content.ReadFromJsonAsync<AnalysisResponse>();
        Assert.NotNull(analysis);
        Assert.NotEqual(Guid.Empty, analysis.AnalysisId);
        Assert.Equal(8.76m, analysis.Draft.Amount);
        Assert.Equal(new DateOnly(2026, 10, 12), analysis.Draft.ExpenseDate);
        Assert.Contains("ALDI", analysis.Draft.Merchant ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Equal(CategorySeed.Groceries, analysis.Draft.CategoryId);
        Assert.True(analysis.Draft.Currency is null or "AUD");
        Assert.Equal(0, await CountExpensesAsync());
    }

    // Invalid files are rejected before anything is sent to Groq.
    [Fact]
    public async Task Analyze_TextFileDeclaredAsPng_Returns400()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.PostAsync("/api/receipts/analyze",
            ImageForm(Encoding.UTF8.GetBytes("not an image"), "receipt.png", "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("file"));
        Assert.Equal(0, await CountExpensesAsync());
    }

    [Fact]
    public async Task Analyze_Gif_Returns400()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);

        var response = await client.PostAsync("/api/receipts/analyze",
            ImageForm([.. "GIF89a"u8, 0, 0, 0, 0], "receipt.gif", "image/gif"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountExpensesAsync());
    }

    [Fact]
    public async Task Analyze_FileOver3MB_Returns413()
    {
        using var client = Factory.CreateClientFor(TestUsers.UserA);
        var image = new byte[4 * 1024 * 1024];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(image, 0);

        var response = await client.PostAsync("/api/receipts/analyze", ImageForm(image, "receipt.png", "image/png"));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal(0, await CountExpensesAsync());
    }

    private static MultipartFormDataContent ImageForm(byte[] content, string fileName, string contentType)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private async Task<int> CountExpensesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Expenses.CountAsync();
    }

    private sealed record AnalysisResponse(Guid AnalysisId, DraftResponse Draft);

    private sealed record DraftResponse(string? Merchant, decimal? Amount, string? Currency, DateOnly? ExpenseDate, Guid? CategoryId);
}
