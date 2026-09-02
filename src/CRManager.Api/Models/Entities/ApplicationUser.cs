using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace CRManager.Api.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<CreditCard> CreditCards { get; set; } = new List<CreditCard>();
}
