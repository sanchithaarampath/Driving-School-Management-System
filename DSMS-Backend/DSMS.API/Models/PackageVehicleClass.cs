using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class PackageVehicleClass
{
    public int Id { get; set; }

    public int PackageHeaderId { get; set; }

    public int VehicleClassId { get; set; }

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual Package PackageHeader { get; set; } = null!;

    public virtual VehicleClass VehicleClass { get; set; } = null!;
}
