using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSMS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingDaysToCoursePackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrainingDays",
                table: "CoursePackages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrainingDays",
                table: "CoursePackages");
        }
    }
}
