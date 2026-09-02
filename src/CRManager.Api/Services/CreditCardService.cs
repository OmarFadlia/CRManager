using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CRManager.Api.Data;
using CRManager.Api.Models.Entities;
using CRManager.Shared;
using CRManager.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRManager.Api.Services;

public class CreditCardService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : ICreditCardService
{
    private string GetCurrentUserId()
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Authenticated user ID not found in current context.");
        }
        return userId;
    }

    public static DateTime CalculateSettlementDate(DateTime transactionDate, int settlementDay)
    {
        var validDay = (settlementDay <= 0 || settlementDay > 31) ? 25 : settlementDay;
        var maxDays = DateTime.DaysInMonth(transactionDate.Year, transactionDate.Month);
        var safeDay = Math.Clamp(validDay, 1, maxDays);

        var baseDate = new DateTime(transactionDate.Year, transactionDate.Month, safeDay);
        if (transactionDate.Day < safeDay)
        {
            return baseDate.AddMonths(1);
        }

        return baseDate.AddMonths(2);
    }

    public async Task<List<CreditCardSummaryDto>> GetSummariesAsync()
    {
        var userId = GetCurrentUserId();

        var creditCards = await context.CreditCards
            .Where(c => c.UserId == userId)
            .Include(c => c.Transactions)
            .ToListAsync();

        return creditCards.Select(MapToSummaryDto).ToList();
    }

    public async Task<CreditCardSummaryDto?> GetSummaryAsync(Guid id)
    {
        var userId = GetCurrentUserId();

        var creditCard = await context.CreditCards
            .Where(c => c.UserId == userId && c.Id == id)
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync();

        if (creditCard == null)
            return null;

        return MapToSummaryDto(creditCard);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var cards = await GetSummariesAsync();

        var totalDebt = cards.Sum(c => c.TotalDebt);
        var totalLimit = cards.Sum(c => c.CreditLimit);
        var totalAvailable = cards.Sum(c => c.AvailableCredit);
        var totalNextSettlement = cards.Sum(c => c.NextSettlementAmount);

        var activeSettlementDates = cards
            .Where(c => c.NextSettlementDate.HasValue)
            .Select(c => c.NextSettlementDate!.Value)
            .ToList();

        DateTime? nearestSettlement = activeSettlementDates.Count > 0
            ? activeSettlementDates.Min()
            : null;

        return new DashboardSummaryDto
        {
            TotalDebt = totalDebt,
            TotalCreditLimit = totalLimit,
            TotalAvailableCredit = totalAvailable,
            TotalNextSettlementAmount = totalNextSettlement,
            NearestSettlementDate = nearestSettlement,
            Cards = cards
        };
    }

    public async Task<CreditCardSummaryDto> CreateCardAsync(CreateCreditCardRequest request)
    {
        var userId = GetCurrentUserId();

        var card = new CreditCard
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BankName = string.IsNullOrWhiteSpace(request.BankName) ? "CIB" : request.BankName.Trim(),
            CreditLimit = request.CreditLimit,
            SettlementDay = (request.SettlementDay <= 0 || request.SettlementDay > 31) ? 25 : request.SettlementDay
        };

        context.CreditCards.Add(card);
        await context.SaveChangesAsync();

        return MapToSummaryDto(card);
    }

    public async Task<CreditCardSummaryDto?> UpdateCardAsync(Guid id, UpdateCreditCardRequest request)
    {
        var userId = GetCurrentUserId();

        var card = await context.CreditCards
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (card == null)
            return null;

        if (!string.IsNullOrWhiteSpace(request.BankName))
        {
            card.BankName = request.BankName.Trim();
        }

        if (request.CreditLimit > 0)
        {
            card.CreditLimit = request.CreditLimit;
        }

        card.SettlementDay = (request.SettlementDay <= 0 || request.SettlementDay > 31) ? 25 : request.SettlementDay;

        await context.SaveChangesAsync();

        return MapToSummaryDto(card);
    }

    public async Task<bool> DeleteCardAsync(Guid id)
    {
        var userId = GetCurrentUserId();

        var card = await context.CreditCards
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (card == null)
            return false;

        context.CreditCards.Remove(card);
        await context.SaveChangesAsync();
        return true;
    }

    private static CreditCardSummaryDto MapToSummaryDto(CreditCard creditCard)
    {
        var purchases = creditCard.Transactions
            .Where(t => t.Type == TransactionType.Purchase)
            .ToList();

        var payments = creditCard.Transactions
            .Where(t => t.Type == TransactionType.Payment)
            .ToList();

        var totalPurchases = purchases.Sum(t => t.Amount);
        var totalPayments = payments.Sum(t => t.Amount);

        // Debt = purchases - payments (clamped to min 0)
        var totalDebt = Math.Max(0m, totalPurchases - totalPayments);
        var availableCredit = creditCard.CreditLimit - totalDebt;

        DateTime? nextSettlementDate = null;
        decimal nextSettlementAmount = 0m;

        if (totalDebt > 0 && purchases.Count > 0)
        {
            nextSettlementDate = purchases
                .Select(t => CalculateSettlementDate(t.Date, creditCard.SettlementDay))
                .Min();

            if (nextSettlementDate.HasValue)
            {
                // Unsettled purchase dues for the earliest upcoming cycle
                var cyclePurchases = purchases
                    .Where(t => CalculateSettlementDate(t.Date, creditCard.SettlementDay) == nextSettlementDate.Value)
                    .Sum(t => t.Amount);

                nextSettlementAmount = Math.Min(totalDebt, cyclePurchases);
            }
        }

        return new CreditCardSummaryDto
        {
            Id = creditCard.Id,
            BankName = creditCard.BankName,
            CreditLimit = creditCard.CreditLimit,
            SettlementDay = creditCard.SettlementDay,
            TotalDebt = totalDebt,
            AvailableCredit = availableCredit,
            NextSettlementDate = nextSettlementDate,
            NextSettlementAmount = nextSettlementAmount
        };
    }
}
