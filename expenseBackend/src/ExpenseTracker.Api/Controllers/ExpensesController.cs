using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/expenses")]
public sealed class ExpensesController(IExpenseService expenses) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ExpenseDto>> List([FromQuery] ExpenseListQuery query, CancellationToken cancellationToken) =>
        expenses.ListAsync(query, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create(ExpenseRequest request, CancellationToken cancellationToken)
    {
        var expense = await expenses.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = expense.Id }, expense);
    }

    [HttpGet("{id:guid}")]
    public Task<ExpenseDto> Get(Guid id, CancellationToken cancellationToken) =>
        expenses.GetAsync(id, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<ExpenseDto> Update(Guid id, ExpenseRequest request, CancellationToken cancellationToken) =>
        expenses.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await expenses.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
