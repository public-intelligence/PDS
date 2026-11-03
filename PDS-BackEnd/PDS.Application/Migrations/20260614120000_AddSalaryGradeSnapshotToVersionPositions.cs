using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <summary>
    /// RF-RE-03/RF-RE-04: agrega la fotografía de salario y grado a StructureVersionPositions para que
    /// el costo estimado y la categoría salarial (= grado) sean fieles a cada versión publicada, sin
    /// depender de la entidad operativa Position. Cambio aditivo: no altera la semántica existente.
    /// </summary>
    /// <inheritdoc />
    public partial class AddSalaryGradeSnapshotToVersionPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Grade",
                schema: "Organization",
                table: "StructureVersionPositions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MinSalary",
                schema: "Organization",
                table: "StructureVersionPositions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxSalary",
                schema: "Organization",
                table: "StructureVersionPositions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Backfill de versiones ya publicadas desde el cargo operativo vigente (mejor esfuerzo).
            // Las filas cuyo cargo fue borrado después quedan en 0 (brecha documentada para versiones
            // pre-existentes; las versiones nuevas fotografían el valor al publicar).
            migrationBuilder.Sql(@"
UPDATE svp
SET svp.[Grade]     = p.[Grade],
    svp.[MinSalary] = p.[MinSalary],
    svp.[MaxSalary] = p.[MaxSalary]
FROM [Organization].[StructureVersionPositions] svp
INNER JOIN [Organization].[Positions] p ON p.[Id] = svp.[PositionId];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Grade",
                schema: "Organization",
                table: "StructureVersionPositions");

            migrationBuilder.DropColumn(
                name: "MinSalary",
                schema: "Organization",
                table: "StructureVersionPositions");

            migrationBuilder.DropColumn(
                name: "MaxSalary",
                schema: "Organization",
                table: "StructureVersionPositions");
        }
    }
}
