using System;

namespace DSMS.API.Models;

public partial class StudentVehicleClass
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    // Vehicle class codes: A1, A, B1, B_Auto, B_Manual, C1, C, CE, D1, D, G1, G, J
    public string VehicleClassCode { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
