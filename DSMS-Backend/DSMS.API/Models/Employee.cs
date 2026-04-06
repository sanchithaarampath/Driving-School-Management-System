using System;

namespace DSMS.API.Models;

public partial class Employee
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int BranchId { get; set; }

    public string EmployeeName { get; set; } = null!;

    public string Nic { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string? Designation { get; set; }

    public string? Department { get; set; }

    public DateTime? JoinDate { get; set; }

    public string? Address { get; set; }

    public string? EmergencyContact { get; set; }

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}
