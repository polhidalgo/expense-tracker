using System.Net;
using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Infrastructure.AI;

namespace ExpenseTracker.UnitTests.Receipts;

public class GroqAnswerTests
{
    [Fact]
    public void ReadAnswer_CorrectJson_ReadsAllFields()
    {
        var raw = GroqReceiptExtractor.ReadAnswer(
            """{"merchant":"Test Market","amount":23.45,"currency":"AUD","date":"2026-10-15","category":"Groceries"}""");

        Assert.Equal(new RawReceipt("Test Market", 23.45m, "AUD", "2026-10-15", "Groceries"), raw);
    }

    // Missing fields, nulls and wrong types stay null. An amount written as text is still read.
    [Fact]
    public void ReadAnswer_MissingOrWrongFields_StayNull()
    {
        var raw = GroqReceiptExtractor.ReadAnswer("""{"merchant":null,"amount":"23.45","date":20261015}""");

        Assert.Equal(new RawReceipt(null, 23.45m, null, null, null), raw);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1, 2, 3]")]
    [InlineData("")]
    public void ReadAnswer_InvalidJson_IsInvalidResponse(string content)
    {
        var ex = Assert.Throws<ReceiptExtractionException>(() => GroqReceiptExtractor.ReadAnswer(content));

        Assert.Equal(ReceiptFailure.InvalidResponse, ex.Reason);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, ReceiptFailure.RateLimited)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ReceiptFailure.Unavailable)]
    [InlineData(HttpStatusCode.InternalServerError, ReceiptFailure.Unavailable)]
    [InlineData(HttpStatusCode.Unauthorized, ReceiptFailure.Unavailable)]
    public void ToFailure_MapsGroqStatus(HttpStatusCode status, ReceiptFailure expected)
    {
        Assert.Equal(expected, GroqReceiptExtractor.ToFailure(status));
    }
}
