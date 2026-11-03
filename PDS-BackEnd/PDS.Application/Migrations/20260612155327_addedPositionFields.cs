using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class addedPositionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Characteristics",
                schema: "Organization",
                table: "Positions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NatureOfWork",
                schema: "Organization",
                table: "Positions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredEducation",
                schema: "Organization",
                table: "Positions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypicalTasks",
                schema: "Organization",
                table: "Positions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Characteristics",
                schema: "Organization",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "NatureOfWork",
                schema: "Organization",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "RequiredEducation",
                schema: "Organization",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "TypicalTasks",
                schema: "Organization",
                table: "Positions");
        }
    }
}
