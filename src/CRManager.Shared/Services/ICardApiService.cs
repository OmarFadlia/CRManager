using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRManager.Shared.DTOs;

namespace CRManager.Shared.Services;

public interface ICardApiService
{
    Task<List<CreditCardSummaryDto>> GetCardsAsync();
    Task<CreditCardSummaryDto?> GetCardByIdAsync(Guid id);
    Task<DashboardSummaryDto?> GetDashboardSummaryAsync();
    Task<CreditCardSummaryDto?> CreateCardAsync(CreateCreditCardRequest request);
    Task<CreditCardSummaryDto?> UpdateCardAsync(Guid id, UpdateCreditCardRequest request);
    Task<bool> DeleteCardAsync(Guid id);

    Task<List<TransactionDto>> GetTransactionsAsync(Guid? cardId = null);
    Task<TransactionDto?> LogTransactionAsync(CreateTransactionRequest request);
    Task<TransactionDto?> ToggleTransactionPaidAsync(Guid id);
    Task<bool> DeleteTransactionAsync(Guid id);
}
