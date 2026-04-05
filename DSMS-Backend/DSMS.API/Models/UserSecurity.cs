using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class UserSecurity
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? UserFullName { get; set; }

    public int RoleId { get; set; }

    public bool? Active { get; set; }

    public bool? FirstTimeLogin { get; set; }

    public DateTime? PasswordExpiration { get; set; }

    public string? ActiveStatusChangedBy { get; set; }

    public DateTime? ActiveStatusChangedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual User? User { get; set; }
}
