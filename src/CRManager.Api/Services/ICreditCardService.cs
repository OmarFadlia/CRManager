using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRManager.Shared.DTOs;

namespace CRManager.Api.Services;

public interface ICreditCardService
{
    Task<List<CreditCardSummaryDto>> GetSummariesAsync();
    Task<CreditCardSummaryDto?> GetSummaryAsync(Guid id);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task<CreditCardSummaryDto> CreateCardAsync(CreateCreditCardRequest request);
    Task<CreditCardSummaryDto?> UpdateCardAsync(Guid id, UpdateCreditCardRequest request);
    Task<bool> DeleteCardAsync(Guid id);
}
