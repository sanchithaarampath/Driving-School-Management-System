using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class Package
{
    public int Id { get; set; }

    public string PackageName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int TotLectureHours { get; set; }

    public int TotTrainingHours { get; set; }

    public decimal ChargePerExtraHour { get; set; }

    public decimal Rmvcharges { get; set; }

    public decimal DownPaymentAmount { get; set; }

    public decimal Price { get; set; }

    public decimal MaxDiscount { get; set; }

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual ICollection<PackageVehicleClass> PackageVehicleClasses { get; set; } = new List<PackageVehicleClass>();

    public virtual ICollection<StudentPackageRegistration> StudentPackageRegistrations { get; set; } = new List<StudentPackageRegistration>();
}
