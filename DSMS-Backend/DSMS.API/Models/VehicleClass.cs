using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class VehicleClass
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string VehicleTypeId { get; set; } = null!;

    public int TrainingHours { get; set; }

    public int LectureHours { get; set; }

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual ICollection<PackageVehicleClass> PackageVehicleClasses { get; set; } = new List<PackageVehicleClass>();
}
