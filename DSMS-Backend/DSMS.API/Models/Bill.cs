using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class Bill
{
    public int Id { get; set; }

    public string BillNumber { get; set; } = null!;

    public int StudentId { get; set; }

    public int StudentPackageRegistrationId { get; set; }

    public DateTime BillDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal NetAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal BalanceAmount { get; set; }

    public string Status { get; set; } = null!;

    public string? Remarks { get; set; }

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Student Student { get; set; } = null!;

    public virtual StudentPackageRegistration StudentPackageRegistration { get; set; } = null!;
}
