using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/budget")]
public sealed class BudgetController(IBudgetService budget) : ControllerBase
{
    [HttpGet]
    public Task<BudgetDto> Get(CancellationToken cancellationToken) =>
        budget.GetAsync(cancellationToken);

    [HttpPut]
    public Task<BudgetDto> Set(BudgetRequest request, CancellationToken cancellationToken) =>
        budget.SetAsync(request, cancellationToken);

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        await budget.DeleteAsync(cancellationToken);
        return NoContent();
    }
}
