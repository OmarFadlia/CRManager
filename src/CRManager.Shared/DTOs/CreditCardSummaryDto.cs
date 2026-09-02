using System;

namespace CRManager.Shared.DTOs;

public class CreditCardSummaryDto
{
    public Guid Id { get; set; }

    public string BankName { get; set; } = string.Empty;

    public decimal CreditLimit { get; set; }

    public int SettlementDay { get; set; }

    public decimal TotalDebt { get; set; }

    public decimal AvailableCredit { get; set; }

    public DateTime? NextSettlementDate { get; set; }

    public decimal NextSettlementAmount { get; set; }

    public decimal UtilizationPercentage => CreditLimit > 0 ? Math.Round((TotalDebt / CreditLimit) * 100, 1) : 0;
}

public class CreateCreditCardRequest
{
    public string BankName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public int SettlementDay { get; set; }
}

public class UpdateCreditCardRequest
{
    public string BankName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public int SettlementDay { get; set; }
}
