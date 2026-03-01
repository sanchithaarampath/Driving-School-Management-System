using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int BillId { get; set; }

    public int StudentId { get; set; }

    public DateTime PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? ReferenceNo { get; set; }

    public string? Remarks { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual Bill Bill { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
