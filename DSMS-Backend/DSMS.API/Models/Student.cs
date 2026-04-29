using System;
using System.Collections.Generic;

namespace DSMS.API.Models;

public partial class Student
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    public string StudentName { get; set; } = null!;

    public string? Email { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string? WhatsAppNumber { get; set; }

    public string Address { get; set; } = null!;

    public string Nic { get; set; } = null!;

    public DateTime Dob { get; set; }

    public string? Gender { get; set; }

    public string? NearestPoliceStation { get; set; }

    public string? NearestDivisionalSecretariat { get; set; }

    public string? ExistingLicenseNo { get; set; }

    public bool? IsSpecialRequirements { get; set; }

    public int? SpecialRequirementTypeId { get; set; }

    public string? PostalCode { get; set; }

    public string? PackageType { get; set; } // "FullCoursework" | "SemiCoursework"

    /// <summary>The Course Package selected at registration (links to pricing)</summary>
    public int? CoursePackageId { get; set; }

    // Document checklist
    public bool? HasBirthCertificate { get; set; }

    public bool? HasNtmiMedical { get; set; }

    public bool? HasNicCopy { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public bool? Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDateTime { get; set; }

    public virtual CoursePackage? CoursePackage { get; set; }

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual Branch Branch { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<StudentDocument> StudentDocuments { get; set; } = new List<StudentDocument>();

    public virtual ICollection<StudentPackageRegistration> StudentPackageRegistrations { get; set; } = new List<StudentPackageRegistration>();
}
