using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class User
{
    public int Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public int RoleId { get; set; }

    public string? ContactNo { get; set; }

    public string? Email { get; set; }

    public string? Department { get; set; }

    public string? Designation { get; set; }

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<UserSecurity> UserSecurities { get; set; } = new List<UserSecurity>();
}
