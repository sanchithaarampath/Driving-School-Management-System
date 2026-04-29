using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSMS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCoursePackageToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoursePackageId",
                table: "Student",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Student_CoursePackageId",
                table: "Student",
                column: "CoursePackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_CoursePackage",
                table: "Student",
                column: "CoursePackageId",
                principalTable: "CoursePackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Student_CoursePackage",
                table: "Student");

            migrationBuilder.DropIndex(
                name: "IX_Student_CoursePackageId",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "CoursePackageId",
                table: "Student");
        }
    }
}
