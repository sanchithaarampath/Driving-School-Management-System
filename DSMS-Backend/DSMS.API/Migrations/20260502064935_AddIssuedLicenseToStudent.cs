using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSMS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIssuedLicenseToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IssuedLicenseNo",
                table: "Student",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LicenseIssuedDate",
                table: "Student",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssuedLicenseNo",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "LicenseIssuedDate",
                table: "Student");
        }
    }
}
