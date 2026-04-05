using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class RequiredDocument
{
    public int Id { get; set; }

    public string DocumentName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual ICollection<StudentDocument> StudentDocuments { get; set; } = new List<StudentDocument>();
}
