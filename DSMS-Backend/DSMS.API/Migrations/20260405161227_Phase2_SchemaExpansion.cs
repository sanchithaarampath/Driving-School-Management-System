using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSMS.API.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_SchemaExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "UserSecurity",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBirthCertificate",
                table: "Student",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasNicCopy",
                table: "Student",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasNtmiMedical",
                table: "Student",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageType",
                table: "Student",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Student",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentVehicleClass",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    VehicleClassCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentVehicleClass", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SVC_Student",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrainingAttendance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentPackageRegistrationId = table.Column<int>(type: "int", nullable: false),
                    InstructorId = table.Column<int>(type: "int", nullable: true),
                    AttendanceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DayNumber = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsReadyForPracticalTest = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TA_Instructor",
                        column: x => x.InstructorId,
                        principalTable: "Instructor",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TA_SPR",
                        column: x => x.StudentPackageRegistrationId,
                        principalTable: "StudentPackageRegistration",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurity_BranchId",
                table: "UserSecurity",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentVehicleClass_StudentId",
                table: "StudentVehicleClass",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAttendance_InstructorId",
                table: "TrainingAttendance",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAttendance_StudentPackageRegistrationId",
                table: "TrainingAttendance",
                column: "StudentPackageRegistrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSecurity_Branch",
                table: "UserSecurity",
                column: "BranchId",
                principalTable: "Branch",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSecurity_Branch",
                table: "UserSecurity");

            migrationBuilder.DropTable(
                name: "StudentVehicleClass");

            migrationBuilder.DropTable(
                name: "TrainingAttendance");

            migrationBuilder.DropIndex(
                name: "IX_UserSecurity_BranchId",
                table: "UserSecurity");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "UserSecurity");

            migrationBuilder.DropColumn(
                name: "HasBirthCertificate",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "HasNicCopy",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "HasNtmiMedical",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "PackageType",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Student");
        }
    }
}
