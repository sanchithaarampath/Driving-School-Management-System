using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class SpecialRequirementType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }
}
