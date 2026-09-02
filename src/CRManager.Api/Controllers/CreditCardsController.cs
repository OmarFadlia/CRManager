using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRManager.Api.Services;
using CRManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CreditCardsController(ICreditCardService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CreditCardSummaryDto>>> GetCreditCards()
    {
        var summaries = await service.GetSummariesAsync();
        return Ok(summaries);
    }

    [HttpGet("{id:guid}")]
    [HttpGet("{id:guid}/summary")]
    public async Task<ActionResult<CreditCardSummaryDto>> GetSummary(Guid id)
    {
        var summary = await service.GetSummaryAsync(id);
        if (summary == null)
            return NotFound();

        return Ok(summary);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary()
    {
        var dashboard = await service.GetDashboardSummaryAsync();
        return Ok(dashboard);
    }

    [HttpPost]
    public async Task<ActionResult<CreditCardSummaryDto>> CreateCreditCard([FromBody] CreateCreditCardRequest request)
    {
        var card = await service.CreateCardAsync(request);
        return CreatedAtAction(nameof(GetSummary), new { id = card.Id }, card);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CreditCardSummaryDto>> UpdateCreditCard(Guid id, [FromBody] UpdateCreditCardRequest request)
    {
        var card = await service.UpdateCardAsync(id, request);
        if (card == null)
            return NotFound();

        return Ok(card);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCreditCard(Guid id)
    {
        var deleted = await service.DeleteCardAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
