using System;

namespace DSMS.API.Models;

public partial class TrainingSession
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int InstructorId { get; set; }
    public int BranchId { get; set; }

    public DateTime ScheduledDate { get; set; }
    public string ScheduledTime { get; set; } = "";          // "09:00"
    public decimal DurationHours { get; set; } = 1;
    public string VehicleClass { get; set; } = "";

    // Scheduled | Completed | Cancelled | NoShow
    public string Status { get; set; } = "Scheduled";

    public string? SessionNotes { get; set; }
    public decimal? HoursLogged { get; set; }
    public string? CancelReason { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedDateTime { get; set; }

    public virtual Student Student { get; set; } = null!;
    public virtual Instructor Instructor { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
