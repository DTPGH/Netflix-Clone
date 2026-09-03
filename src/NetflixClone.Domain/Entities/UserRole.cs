using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class UserRole
{
    public int Id { get; set; }

    public int UserAccountId { get; set; }

    public int RoleId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual UserAccount UserAccount { get; set; } = null!;
}
