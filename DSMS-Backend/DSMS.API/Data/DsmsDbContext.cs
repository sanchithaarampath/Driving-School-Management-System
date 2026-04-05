using System;
using System.Collections.Generic;
using DSMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DSMS.API.Data;

public partial class DsmsDbContext : DbContext
{
    public DsmsDbContext(DbContextOptions<DsmsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Bill> Bills { get; set; }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<Instructor> Instructors { get; set; }

    public virtual DbSet<Package> Packages { get; set; }

    public virtual DbSet<PackageVehicleClass> PackageVehicleClasses { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<RequiredDocument> RequiredDocuments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SpecialRequirementType> SpecialRequirementTypes { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentClassProgress> StudentClassProgresses { get; set; }

    public virtual DbSet<StudentDocument> StudentDocuments { get; set; }

    public virtual DbSet<StudentPackageRegistration> StudentPackageRegistrations { get; set; }

    public virtual DbSet<StudentPracticalTestAttempt> StudentPracticalTestAttempts { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserSecurity> UserSecurities { get; set; }

    public virtual DbSet<VehicleClass> VehicleClasses { get; set; }

    public virtual DbSet<VehicleType> VehicleTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Bill__3214EC077A4C266D");

            entity.ToTable("Bill");

            entity.Property(e => e.BalanceAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BillNumber).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.NetAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Remarks).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Student).WithMany(p => p.Bills)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bill_Student");

            entity.HasOne(d => d.StudentPackageRegistration).WithMany(p => p.Bills)
                .HasForeignKey(d => d.StudentPackageRegistrationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bill_SPR");
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Branch__3214EC070DD6A432");

            entity.ToTable("Branch");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
        });

        modelBuilder.Entity<Instructor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Instruct__3214EC0722CA1438");

            entity.ToTable("Instructor");

            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.InstructorName).HasMaxLength(200);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.LicenseNo).HasMaxLength(100);
            entity.Property(e => e.Nic)
                .HasMaxLength(20)
                .HasColumnName("NIC");
            entity.Property(e => e.Phone).HasMaxLength(50);

            entity.HasOne(d => d.Branch).WithMany(p => p.Instructors)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Instructor_Branch");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Package__3214EC07B375D793");

            entity.ToTable("Package");

            entity.Property(e => e.ChargePerExtraHour)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("CHargePerExtraHour");
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DownPaymentAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.MaxDiscount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PackageName).HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Rmvcharges)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("RMVCharges");
        });

        modelBuilder.Entity<PackageVehicleClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PackageV__3214EC07B4A9C963");

            entity.ToTable("PackageVehicleClass");

            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);

            entity.HasOne(d => d.PackageHeader).WithMany(p => p.PackageVehicleClasses)
                .HasForeignKey(d => d.PackageHeaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PVC_Package");

            entity.HasOne(d => d.VehicleClass).WithMany(p => p.PackageVehicleClasses)
                .HasForeignKey(d => d.VehicleClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PVC_VehicleClass");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payment__3214EC07D92CDDB7");

            entity.ToTable("Payment");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.ReferenceNo).HasMaxLength(100);
            entity.Property(e => e.Remarks).HasMaxLength(500);

            entity.HasOne(d => d.Bill).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Bill");

            entity.HasOne(d => d.Student).WithMany(p => p.Payments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Student");
        });

        modelBuilder.Entity<RequiredDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Required__3214EC07C897CF9C");

            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DocumentName).HasMaxLength(200);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Role__3214EC07D664BE92");

            entity.ToTable("Role");

            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.RoleName).HasMaxLength(100);
        });

        modelBuilder.Entity<SpecialRequirementType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SpecialR__3214EC074BBFFE7A");

            entity.ToTable("SpecialRequirementType");

            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student__3214EC07DFCEA494");

            entity.ToTable("Student");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.ExistingLicenseNo).HasMaxLength(50);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.NearestDivisionalSecretariat).HasMaxLength(200);
            entity.Property(e => e.NearestPoliceStation).HasMaxLength(200);
            entity.Property(e => e.Nic)
                .HasMaxLength(20)
                .HasColumnName("NIC");
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.StudentName).HasMaxLength(200);
            entity.Property(e => e.WhatsAppNumber).HasMaxLength(50);

            entity.HasOne(d => d.Branch).WithMany(p => p.Students)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Student_Branch");
        });

        modelBuilder.Entity<StudentClassProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentC__3214EC07AA5E8745");

            entity.ToTable("StudentClassProgress");

            entity.Property(e => e.HoursCompleted).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(d => d.Instructor).WithMany(p => p.StudentClassProgresses)
                .HasForeignKey(d => d.InstructorId)
                .HasConstraintName("FK_SCP_Instructor");

            entity.HasOne(d => d.StudentPackageRegistration).WithMany(p => p.StudentClassProgresses)
                .HasForeignKey(d => d.StudentPackageRegistrationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SCP_SPR");
        });

        modelBuilder.Entity<StudentDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentD__3214EC0751AB05B8");

            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);

            entity.HasOne(d => d.RequiredDocument).WithMany(p => p.StudentDocuments)
                .HasForeignKey(d => d.RequiredDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentDocs_RequiredDocs");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentDocuments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentDocs_Student");
        });

        modelBuilder.Entity<StudentPackageRegistration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentP__3214EC078C9EC726");

            entity.ToTable("StudentPackageRegistration");

            entity.Property(e => e.BalanceAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ExamStatus).HasMaxLength(50);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.PackageHeader).WithMany(p => p.StudentPackageRegistrations)
                .HasForeignKey(d => d.PackageHeaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SPR_Package");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentPackageRegistrations)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SPR_Student");
        });

        modelBuilder.Entity<StudentPracticalTestAttempt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentP__3214EC07022D1477");

            entity.ToTable("StudentPracticalTestAttempt");

            entity.Property(e => e.Remarks).HasMaxLength(500);
            entity.Property(e => e.Result).HasMaxLength(50);

            entity.HasOne(d => d.StudentClassProgress).WithMany(p => p.StudentPracticalTestAttempts)
                .HasForeignKey(d => d.StudentClassProgressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SPTA_SCP");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3214EC0705BB4141");

            entity.ToTable("User");

            entity.Property(e => e.ContactNo).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Designation).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Role");
        });

        modelBuilder.Entity<UserSecurity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserSecu__3214EC073343E162");

            entity.ToTable("UserSecurity");

            entity.Property(e => e.ActiveStatusChangedBy).HasMaxLength(100);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(500);
            entity.Property(e => e.UserFullName).HasMaxLength(200);
            entity.Property(e => e.UserName).HasMaxLength(100);

            entity.HasOne(d => d.Role).WithMany(p => p.UserSecurities)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserSecurity_Role");

            entity.HasOne(d => d.User).WithMany(p => p.UserSecurities)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserSecurity_User");
        });

        modelBuilder.Entity<VehicleClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VehicleC__3214EC0779DE42EA");

            entity.ToTable("VehicleClass");

            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.VehicleTypeId).HasMaxLength(50);
        });

        modelBuilder.Entity<VehicleType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VehicleT__3214EC079450D4A5");

            entity.ToTable("VehicleType");

            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.VehicleName).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
