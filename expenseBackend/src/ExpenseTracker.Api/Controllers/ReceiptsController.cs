using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/receipts")]
public sealed class ReceiptsController(IReceiptService receipts) : ControllerBase
{
    [HttpPost("analyze")]
    public async Task<ReceiptDraft> Analyze(IFormFile file, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return await receipts.AnalyzeAsync(stream.ToArray(), cancellationToken);
    }
}
