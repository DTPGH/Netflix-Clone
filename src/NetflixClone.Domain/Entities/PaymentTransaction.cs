using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class PaymentTransaction
{
    public int Id { get; set; }

    public int SubscriptionId { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = null!;

    public string TransactionCode { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Subscription Subscription { get; set; } = null!;
}
