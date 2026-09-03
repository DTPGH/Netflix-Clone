using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class RefreshToken
{
    public int Id { get; set; }

    public int UserAccountId { get; set; }

    public int DeviceId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public int? ReplacedByTokenId { get; set; }

    public virtual Device Device { get; set; } = null!;

    public virtual ICollection<RefreshToken> InverseReplacedByToken { get; set; } = new List<RefreshToken>();

    public virtual RefreshToken? ReplacedByToken { get; set; }

    public virtual UserAccount UserAccount { get; set; } = null!;
}
