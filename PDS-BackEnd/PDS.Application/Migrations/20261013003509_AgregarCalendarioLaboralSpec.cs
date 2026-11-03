using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCalendarioLaboralSpec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CalendarioVersion",
                schema: "Organization",
                table: "Localidades",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<bool>(
                name: "EsLocalidadCalendarioPredeterminada",
                schema: "Organization",
                table: "Localidades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Canal",
                schema: "Organization",
                table: "LocalidadAuditorias",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CalendarioCambios",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CalendarioVersion = table.Column<long>(type: "bigint", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaDesde = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaHasta = table.Column<DateOnly>(type: "date", nullable: true),
                    OcurridoEn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarioCambios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarioCambios_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalendariosFeriados",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaisId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ambito = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LocalidadGestoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendariosFeriados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendariosFeriados_Localidades_LocalidadGestoraId",
                        column: x => x.LocalidadGestoraId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalendariosFeriados_Paises_PaisId",
                        column: x => x.PaisId,
                        principalSchema: "Organization",
                        principalTable: "Paises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadExcepcionesCalendario",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Origen = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReferenciaNormativa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstrumentoLegalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EsSistema = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ModificadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Intervalos = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadExcepcionesCalendario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadExcepcionesCalendario_LegalInstruments_InstrumentoLegalId",
                        column: x => x.InstrumentoLegalId,
                        principalSchema: "Organization",
                        principalTable: "LegalInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadExcepcionesCalendario_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadJornadas",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VigenteDesde = table.Column<DateOnly>(type: "date", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Semana = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadJornadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadJornadas_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalendarioFeriadosAuditorias",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    CalendarioFeriadosId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Canal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarioFeriadosAuditorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarioFeriadosAuditorias_CalendariosFeriados_CalendarioFeriadosId",
                        column: x => x.CalendarioFeriadosId,
                        principalSchema: "Organization",
                        principalTable: "CalendariosFeriados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalendarioFeriadosDias",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    CalendarioFeriadosId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Alcance = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReferenciaNormativa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Intervalos = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarioFeriadosDias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarioFeriadosDias_CalendariosFeriados_CalendarioFeriadosId",
                        column: x => x.CalendarioFeriadosId,
                        principalSchema: "Organization",
                        principalTable: "CalendariosFeriados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadCalendariosFeriados",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CalendarioFeriadosId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VigenteDesde = table.Column<DateOnly>(type: "date", nullable: true),
                    VigenteHasta = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadCalendariosFeriados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadCalendariosFeriados_CalendariosFeriados_CalendarioFeriadosId",
                        column: x => x.CalendarioFeriadosId,
                        principalSchema: "Organization",
                        principalTable: "CalendariosFeriados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadCalendariosFeriados_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_CalendarioPredeterminada",
                schema: "Organization",
                table: "Localidades",
                column: "OrganizationId",
                unique: true,
                filter: "[EsLocalidadCalendarioPredeterminada] = 1 AND [DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarioCambios_LocalidadId",
                schema: "Organization",
                table: "CalendarioCambios",
                column: "LocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarioCambios_OrganizationId_Id",
                schema: "Organization",
                table: "CalendarioCambios",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarioFeriadosAuditorias_CalendarioFeriadosId",
                schema: "Organization",
                table: "CalendarioFeriadosAuditorias",
                column: "CalendarioFeriadosId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarioFeriadosDias_CalendarioFeriadosId_Fecha",
                schema: "Organization",
                table: "CalendarioFeriadosDias",
                columns: new[] { "CalendarioFeriadosId", "Fecha" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarioFeriadosDias_Fecha",
                schema: "Organization",
                table: "CalendarioFeriadosDias",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_CalendariosFeriados_LocalidadGestoraId",
                schema: "Organization",
                table: "CalendariosFeriados",
                column: "LocalidadGestoraId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendariosFeriados_OrganizationId_Codigo",
                schema: "Organization",
                table: "CalendariosFeriados",
                columns: new[] { "OrganizationId", "Codigo" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CalendariosFeriados_PaisId",
                schema: "Organization",
                table: "CalendariosFeriados",
                column: "PaisId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadCalendariosFeriados_CalendarioFeriadosId",
                schema: "Organization",
                table: "LocalidadCalendariosFeriados",
                column: "CalendarioFeriadosId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadCalendariosFeriados_LocalidadId_CalendarioFeriadosId",
                schema: "Organization",
                table: "LocalidadCalendariosFeriados",
                columns: new[] { "LocalidadId", "CalendarioFeriadosId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadExcepcionesCalendario_InstrumentoLegalId",
                schema: "Organization",
                table: "LocalidadExcepcionesCalendario",
                column: "InstrumentoLegalId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadExcepcionesCalendario_LocalidadId_FechaInicio_FechaFin",
                schema: "Organization",
                table: "LocalidadExcepcionesCalendario",
                columns: new[] { "LocalidadId", "FechaInicio", "FechaFin" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadJornadas_LocalidadId_VigenteDesde",
                schema: "Organization",
                table: "LocalidadJornadas",
                columns: new[] { "LocalidadId", "VigenteDesde" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarioCambios",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "CalendarioFeriadosAuditorias",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "CalendarioFeriadosDias",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadCalendariosFeriados",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadExcepcionesCalendario",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadJornadas",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "CalendariosFeriados",
                schema: "Organization");

            migrationBuilder.DropIndex(
                name: "IX_Localidades_CalendarioPredeterminada",
                schema: "Organization",
                table: "Localidades");

            migrationBuilder.DropColumn(
                name: "CalendarioVersion",
                schema: "Organization",
                table: "Localidades");

            migrationBuilder.DropColumn(
                name: "EsLocalidadCalendarioPredeterminada",
                schema: "Organization",
                table: "Localidades");

            migrationBuilder.DropColumn(
                name: "Canal",
                schema: "Organization",
                table: "LocalidadAuditorias");
        }
    }
}
