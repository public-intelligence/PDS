using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarioLaboral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarioCambios",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Entidad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    PaisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ambito = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LocalidadGestoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                name: "LocalidadCalendarioVersiones",
                schema: "Organization",
                columns: table => new
                {
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadCalendarioVersiones", x => x.LocalidadId);
                    table.ForeignKey(
                        name: "FK_LocalidadCalendarioVersiones_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadExcepciones",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: true),
                    Desde = table.Column<TimeOnly>(type: "time", nullable: true),
                    Hasta = table.Column<TimeOnly>(type: "time", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Origen = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EventoCicloVida = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadExcepciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadExcepciones_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadHorarioVersiones",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    VigenteDesde = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadHorarioVersiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadHorarioVersiones_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalendarioFeriadoEntradas",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    CalendarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Alcance = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReferenciaNormativa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrasladoLey13997 = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarioFeriadoEntradas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarioFeriadoEntradas_CalendariosFeriados_CalendarioId",
                        column: x => x.CalendarioId,
                        principalSchema: "Organization",
                        principalTable: "CalendariosFeriados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadCalendarioSuscripciones",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    CalendarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VigenteDesde = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenteHasta = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadCalendarioSuscripciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadCalendarioSuscripciones_CalendariosFeriados_CalendarioId",
                        column: x => x.CalendarioId,
                        principalSchema: "Organization",
                        principalTable: "CalendariosFeriados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadCalendarioSuscripciones_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadExcepcionIntervalos",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ExcepcionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Inicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    Fin = table.Column<TimeOnly>(type: "time", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadExcepcionIntervalos", x => x.Id);
                    table.CheckConstraint("CK_LocalidadExcepcionIntervalos_Rango", "[Inicio] < [Fin]");
                    table.ForeignKey(
                        name: "FK_LocalidadExcepcionIntervalos_LocalidadExcepciones_ExcepcionId",
                        column: x => x.ExcepcionId,
                        principalSchema: "Organization",
                        principalTable: "LocalidadExcepciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadHorarioDias",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadHorarioDias", x => x.Id);
                    table.CheckConstraint("CK_LocalidadHorarioDias_DiaSemana", "[DiaSemana] BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_LocalidadHorarioDias_LocalidadHorarioVersiones_VersionId",
                        column: x => x.VersionId,
                        principalSchema: "Organization",
                        principalTable: "LocalidadHorarioVersiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarioFeriadoEntradaIntervalos",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    EntradaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Inicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    Fin = table.Column<TimeOnly>(type: "time", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarioFeriadoEntradaIntervalos", x => x.Id);
                    table.CheckConstraint("CK_CalendarioFeriadoEntradaIntervalos_Rango", "[Inicio] < [Fin]");
                    table.ForeignKey(
                        name: "FK_CalendarioFeriadoEntradaIntervalos_CalendarioFeriadoEntradas_EntradaId",
                        column: x => x.EntradaId,
                        principalSchema: "Organization",
                        principalTable: "CalendarioFeriadoEntradas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadHorarioIntervalos",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    DiaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Inicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    Fin = table.Column<TimeOnly>(type: "time", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadHorarioIntervalos", x => x.Id);
                    table.CheckConstraint("CK_LocalidadHorarioIntervalos_Rango", "[Inicio] < [Fin]");
                    table.ForeignKey(
                        name: "FK_LocalidadHorarioIntervalos_LocalidadHorarioDias_DiaId",
                        column: x => x.DiaId,
                        principalSchema: "Organization",
                        principalTable: "LocalidadHorarioDias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarioCambios_LocalidadId_Version",
                schema: "Organization",
                table: "CalendarioCambios",
                columns: new[] { "LocalidadId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarioFeriadoEntradaIntervalos_EntradaId",
                schema: "Organization",
                table: "CalendarioFeriadoEntradaIntervalos",
                column: "EntradaId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarioFeriadoEntradas_CalendarioId_Fecha",
                schema: "Organization",
                table: "CalendarioFeriadoEntradas",
                columns: new[] { "CalendarioId", "Fecha" },
                unique: true);

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
                name: "IX_LocalidadCalendarioSuscripciones_CalendarioId",
                schema: "Organization",
                table: "LocalidadCalendarioSuscripciones",
                column: "CalendarioId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadCalendarioSuscripciones_LocalidadId_CalendarioId_VigenteDesde",
                schema: "Organization",
                table: "LocalidadCalendarioSuscripciones",
                columns: new[] { "LocalidadId", "CalendarioId", "VigenteDesde" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadExcepciones_LocalidadId_FechaInicio",
                schema: "Organization",
                table: "LocalidadExcepciones",
                columns: new[] { "LocalidadId", "FechaInicio" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadExcepcionIntervalos_ExcepcionId",
                schema: "Organization",
                table: "LocalidadExcepcionIntervalos",
                column: "ExcepcionId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadHorarioDias_VersionId_DiaSemana",
                schema: "Organization",
                table: "LocalidadHorarioDias",
                columns: new[] { "VersionId", "DiaSemana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadHorarioIntervalos_DiaId",
                schema: "Organization",
                table: "LocalidadHorarioIntervalos",
                column: "DiaId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadHorarioVersiones_LocalidadId_VigenteDesde",
                schema: "Organization",
                table: "LocalidadHorarioVersiones",
                columns: new[] { "LocalidadId", "VigenteDesde" },
                unique: true,
                filter: "[DeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarioCambios",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "CalendarioFeriadoEntradaIntervalos",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadCalendarioSuscripciones",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadCalendarioVersiones",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadExcepcionIntervalos",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadHorarioIntervalos",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "CalendarioFeriadoEntradas",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadExcepciones",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadHorarioDias",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "CalendariosFeriados",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadHorarioVersiones",
                schema: "Organization");
        }
    }
}
