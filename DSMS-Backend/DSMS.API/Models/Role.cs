using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class Role
{
    public int Id { get; set; }

    public string? RoleName { get; set; }

    public string? Description { get; set; }

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual ICollection<UserSecurity> UserSecurities { get; set; } = new List<UserSecurity>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
