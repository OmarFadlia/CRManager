using System;
using CRManager.Shared;

namespace CRManager.Api.Models.Entities;

public class Transaction
{
    public Guid Id { get; set; }

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public TransactionType Type { get; set; } = TransactionType.Purchase;

    public Guid CreditCardId { get; set; }

    public CreditCard CreditCard { get; set; } = null!;
}
