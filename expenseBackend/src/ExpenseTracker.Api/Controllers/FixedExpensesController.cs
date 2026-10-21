using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/fixed-expenses")]
public sealed class FixedExpensesController(IFixedExpenseService fixedExpenses) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<FixedExpenseDto>> List(CancellationToken cancellationToken) =>
        fixedExpenses.ListAsync(cancellationToken);

    [HttpPost]
    public async Task<ActionResult<FixedExpenseDto>> Create(FixedExpenseRequest request, CancellationToken cancellationToken)
    {
        var created = await fixedExpenses.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id:guid}")]
    public Task<FixedExpenseDto> Update(Guid id, FixedExpenseRequest request, CancellationToken cancellationToken) =>
        fixedExpenses.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await fixedExpenses.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
