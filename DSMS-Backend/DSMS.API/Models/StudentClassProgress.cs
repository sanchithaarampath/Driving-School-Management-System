using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class StudentClassProgress
{
    public int Id { get; set; }

    public int StudentPackageRegistrationId { get; set; }

    public int PackageHeaderId { get; set; }

    public int? InstructorId { get; set; }

    public DateTime? SessionDate { get; set; }

    public decimal? HoursCompleted { get; set; }

    public bool? IsTrialFaced { get; set; }

    public int TrialAttempt { get; set; }

    public int Status { get; set; }

    public bool IsDeclined { get; set; }

    public string? Notes { get; set; }

    public virtual Instructor? Instructor { get; set; }

    public virtual StudentPackageRegistration StudentPackageRegistration { get; set; } = null!;

    public virtual ICollection<StudentPracticalTestAttempt> StudentPracticalTestAttempts { get; set; } = new List<StudentPracticalTestAttempt>();
}
