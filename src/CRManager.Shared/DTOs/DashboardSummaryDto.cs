using System;
using System.Collections.Generic;

namespace CRManager.Shared.DTOs;

public class DashboardSummaryDto
{
    public decimal TotalDebt { get; set; }

    public decimal TotalCreditLimit { get; set; }

    public decimal TotalAvailableCredit { get; set; }

    public decimal TotalNextSettlementAmount { get; set; }

    public decimal UtilizationPercentage => TotalCreditLimit > 0 ? Math.Round((TotalDebt / TotalCreditLimit) * 100, 1) : 0;

    public DateTime? NearestSettlementDate { get; set; }

    public List<CreditCardSummaryDto> Cards { get; set; } = new();
}
