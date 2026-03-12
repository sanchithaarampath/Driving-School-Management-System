using System;

namespace DSMS.API.Models;

public partial class TrainingAttendance
{
    public int Id { get; set; }

    public int StudentPackageRegistrationId { get; set; }

    public int? InstructorId { get; set; }

    public DateTime AttendanceDate { get; set; }

    public int DayNumber { get; set; } // 1-15

    public string? Notes { get; set; }

    public bool IsReadyForPracticalTest { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public virtual StudentPackageRegistration StudentPackageRegistration { get; set; } = null!;

    public virtual Instructor? Instructor { get; set; }
}
