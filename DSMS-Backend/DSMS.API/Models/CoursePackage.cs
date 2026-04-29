namespace DSMS.API.Models;

public class CoursePackage
{
    public int Id { get; set; }

    // e.g. "Full Course — Bike + Dual Purpose"
    public string PackageName { get; set; } = string.Empty;

    // "Full Course" | "Semi Course"
    public string CourseType { get; set; } = "Full Course";

    // Comma-separated vehicle class codes the student must have enrolled in
    // e.g. "A1,A"  or  "B_Auto,B_Manual"  or  "A1,A,B_Auto,B_Manual"
    public string VehicleClassCodes { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal MaxDiscount { get; set; }

    public string? Description { get; set; }

    public bool Active { get; set; } = true;

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }
}
