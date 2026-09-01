using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class AdminActionLog
{
    public int Id { get; set; }

    public int ActorUserAccountId { get; set; }

    public int TargetUserAccountId { get; set; }

    public string Action { get; set; } = null!;

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual UserAccount ActorUserAccount { get; set; } = null!;

    public virtual UserAccount TargetUserAccount { get; set; } = null!;
}
