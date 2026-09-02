using System;
using System.Collections.Generic;

namespace CRManager.Api.Models.Entities;

public class CreditCard
{
    public Guid Id { get; set; }

    public string BankName { get; set; } = string.Empty;

    public decimal CreditLimit { get; set; }

    public int SettlementDay { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
