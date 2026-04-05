using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class StudentDocument
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int RequiredDocumentId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual RequiredDocument RequiredDocument { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
