using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class Subscription
{
    public int Id { get; set; }

    public int UserAccountId { get; set; }

    public int PlanId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string Status { get; set; } = null!;

    public bool AutoRenew { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual Plan Plan { get; set; } = null!;

    public virtual UserAccount UserAccount { get; set; } = null!;
}
