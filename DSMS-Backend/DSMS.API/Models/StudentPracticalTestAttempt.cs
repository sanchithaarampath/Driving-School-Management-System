using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class StudentPracticalTestAttempt
{
    public int Id { get; set; }

    public int StudentClassProgressId { get; set; }

    public int AttemptNumber { get; set; }

    public DateTime TestDate { get; set; }

    public string Result { get; set; } = null!;

    public string? Remarks { get; set; }

    public virtual StudentClassProgress StudentClassProgress { get; set; } = null!;
}
