using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class StudentPackageRegistration
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int PackageHeaderId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal BalanceAmount { get; set; }

    public int TotalTrainingHours { get; set; }

    public int CompletedTrainingHours { get; set; }

    public int TotalLectureHours { get; set; }

    public int CompletedLectureHours { get; set; }

    public bool? IsRecommendForTrial { get; set; }

    public bool? IsDocumentSubmitted { get; set; }

    public DateTime? ExamDate { get; set; }

    public int ExamAttempts { get; set; }

    public string? ExamStatus { get; set; }

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual Package PackageHeader { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;

    public virtual ICollection<StudentClassProgress> StudentClassProgresses { get; set; } = new List<StudentClassProgress>();
}
