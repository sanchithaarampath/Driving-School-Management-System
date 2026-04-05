using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class VehicleType
{
    public int Id { get; set; }

    public string VehicleName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }
}
