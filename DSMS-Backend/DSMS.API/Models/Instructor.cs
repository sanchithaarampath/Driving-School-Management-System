using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class Instructor
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int BranchId { get; set; }

    public string InstructorName { get; set; } = null!;

    public string Nic { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string? LicenseNo { get; set; }

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual ICollection<StudentClassProgress> StudentClassProgresses { get; set; } = new List<StudentClassProgress>();
}
