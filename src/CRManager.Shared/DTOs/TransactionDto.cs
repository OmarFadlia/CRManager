using System;

namespace CRManager.Shared.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public TransactionType Type { get; set; } = TransactionType.Purchase;

    public Guid CreditCardId { get; set; }

    public string BankName { get; set; } = string.Empty;

    public DateTime SettlementDate { get; set; }

    // Statement closes on settlement day, payment due on settlement day of the following cycle
    public static DateTime CalculateSettlementDate(DateTime transactionDate, int settlementDay)
    {
        var baseDate = new DateTime(transactionDate.Year, transactionDate.Month, Math.Min(settlementDay, DateTime.DaysInMonth(transactionDate.Year, transactionDate.Month)));
        if (transactionDate.Day < settlementDay)
        {
            return baseDate.AddMonths(1);
        }

        return baseDate.AddMonths(2);
    }
}

public class CreateTransactionRequest
{
    public Guid CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public TransactionType Type { get; set; } = TransactionType.Purchase;
    public bool AllowOverLimit { get; set; } = false;
}
