using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class addedPositionFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Code",
                schema: "Organization",
                table: "Positions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Grade",
                schema: "Organization",
                table: "Positions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OtherDesirableRequirements",
                schema: "Organization",
                table: "Positions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProbatoryPeriod",
                schema: "Organization",
                table: "Positions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Punctuation",
                schema: "Organization",
                table: "Positions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                schema: "Organization",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "Grade",
                schema: "Organization",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "OtherDesirableRequirements",
                schema: "Organization",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "ProbatoryPeriod",
                schema: "Organization",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "Punctuation",
                schema: "Organization",
                table: "Positions");
        }
    }
}
