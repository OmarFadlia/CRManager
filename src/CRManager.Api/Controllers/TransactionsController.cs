using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CRManager.Api.Data;
using CRManager.Api.Models.Entities;
using CRManager.Api.Services;
using CRManager.Shared;
using CRManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransactionsController(ApplicationDbContext context) : ControllerBase
{
    private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions([FromQuery] Guid? cardId)
    {
        var userId = GetCurrentUserId();
        var query = context.Transactions
            .Include(t => t.CreditCard)
            .Where(t => t.CreditCard.UserId == userId)
            .AsQueryable();

        if (cardId.HasValue && cardId.Value != Guid.Empty)
        {
            query = query.Where(t => t.CreditCardId == cardId.Value);
        }

        var list = await query
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        var dtos = list.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        var userId = GetCurrentUserId();
        var card = await context.CreditCards
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == request.CreditCardId && c.UserId == userId);

        if (card == null)
        {
            return NotFound("Credit card not found or access denied.");
        }

        if (request.Amount <= 0)
        {
            return BadRequest("Transaction amount must be greater than 0.");
        }

        var purchases = card.Transactions.Where(t => t.Type == TransactionType.Purchase).Sum(t => t.Amount);
        var payments = card.Transactions.Where(t => t.Type == TransactionType.Payment).Sum(t => t.Amount);
        var totalDebt = Math.Max(0m, purchases - payments);
        var availableCredit = card.CreditLimit - totalDebt;

        if (request.Type == TransactionType.Purchase)
        {
            if (!request.AllowOverLimit && request.Amount > availableCredit)
            {
                return BadRequest($"Purchase amount ({request.Amount:N2} EGP) exceeds available credit ({availableCredit:N2} EGP). Check 'Allow Over-limit' to proceed.");
            }
        }
        else if (request.Type == TransactionType.Payment)
        {
            if (totalDebt <= 0)
            {
                return BadRequest("Card has 0 debt. No payment needed.");
            }
            if (request.Amount > totalDebt)
            {
                return BadRequest($"Payment amount ({request.Amount:N2} EGP) cannot exceed total unpaid debt ({totalDebt:N2} EGP).");
            }
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            Date = request.Date,
            Type = request.Type,
            CreditCardId = request.CreditCardId,
            CreditCard = card
        };

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTransactions), new { cardId = transaction.CreditCardId }, MapToDto(transaction));
    }

    [HttpPut("{id:guid}/toggle-paid")]
    public async Task<ActionResult<TransactionDto>> TogglePaidStatus(Guid id)
    {
        var userId = GetCurrentUserId();
        var transaction = await context.Transactions
            .Include(t => t.CreditCard)
            .FirstOrDefaultAsync(t => t.Id == id && t.CreditCard.UserId == userId);

        if (transaction == null)
        {
            return NotFound("Transaction not found or access denied.");
        }

        transaction.Type = transaction.Type == TransactionType.Purchase ? TransactionType.Payment : TransactionType.Purchase;
        await context.SaveChangesAsync();

        return Ok(MapToDto(transaction));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var userId = GetCurrentUserId();
        var transaction = await context.Transactions
            .Include(t => t.CreditCard)
            .FirstOrDefaultAsync(t => t.Id == id && t.CreditCard.UserId == userId);

        if (transaction == null)
        {
            return NotFound("Transaction not found or access denied.");
        }

        context.Transactions.Remove(transaction);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private static TransactionDto MapToDto(Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            Amount = transaction.Amount,
            Date = transaction.Date,
            Type = transaction.Type,
            CreditCardId = transaction.CreditCardId,
            BankName = transaction.CreditCard?.BankName ?? string.Empty,
            SettlementDate = CreditCardService.CalculateSettlementDate(transaction.Date, transaction.CreditCard?.SettlementDay ?? 25)
        };
    }
}
