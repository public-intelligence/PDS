using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <summary>
    /// Funciones de localidad (RF-LO-45, B30): tablas <c>FuncionesLocalidad</c> y <c>LocalidadFunciones</c> y la semilla de las
    /// cuatro funciones del Apéndice C (script embebido <c>Seed/FuncionesLocalidad.sql</c>, idempotente).
    /// </summary>
    public partial class AddFuncionesLocalidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FuncionesLocalidad",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncionesLocalidad", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadFunciones",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuncionLocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadFunciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadFunciones_FuncionesLocalidad_FuncionLocalidadId",
                        column: x => x.FuncionLocalidadId,
                        principalSchema: "Organization",
                        principalTable: "FuncionesLocalidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadFunciones_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FuncionesLocalidad_Codigo",
                schema: "Organization",
                table: "FuncionesLocalidad",
                column: "Codigo",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FuncionesLocalidad_DeletedAt",
                schema: "Organization",
                table: "FuncionesLocalidad",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadFunciones_FuncionLocalidadId",
                schema: "Organization",
                table: "LocalidadFunciones",
                column: "FuncionLocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadFunciones_LocalidadId_FuncionLocalidadId",
                schema: "Organization",
                table: "LocalidadFunciones",
                columns: new[] { "LocalidadId", "FuncionLocalidadId" },
                unique: true);

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly
                .GetManifestResourceNames()
                .Single(n => n.EndsWith("FuncionesLocalidad.sql", StringComparison.OrdinalIgnoreCase));

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalidadFunciones",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "FuncionesLocalidad",
                schema: "Organization");
        }
    }
}
