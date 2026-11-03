using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalidadesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EstadosLocalidad",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EsTerminal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadosLocalidad", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NaturalezasAdministrativas",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NaturalezasAdministrativas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Paises",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Responsables",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Responsables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Servicios",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposLocalidad",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposLocalidad", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Regiones",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PaisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Regiones_Paises_PaisId",
                        column: x => x.PaisId,
                        principalSchema: "Organization",
                        principalTable: "Paises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Provincias",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PaisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provincias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Provincias_Paises_PaisId",
                        column: x => x.PaisId,
                        principalSchema: "Organization",
                        principalTable: "Paises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Provincias_Regiones_RegionId",
                        column: x => x.RegionId,
                        principalSchema: "Organization",
                        principalTable: "Regiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Municipios",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ProvinciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Municipios_Provincias_ProvinciaId",
                        column: x => x.ProvinciaId,
                        principalSchema: "Organization",
                        principalTable: "Provincias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DistritosMunicipales",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MunicipioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistritosMunicipales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistritosMunicipales_Municipios_MunicipioId",
                        column: x => x.MunicipioId,
                        principalSchema: "Organization",
                        principalTable: "Municipios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Localidades",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NombreOficial = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NombreCorto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Sigla = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TipoLocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NaturalezaAdministrativaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstadoLocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadResponsableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnidadVinculadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocalidadPadreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProvinciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MunicipioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DistritoMunicipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SectorBarrioParaje = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DireccionCompleta = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EdificioPisoLocalReferenciaInterna = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CodigoPostal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Latitud = table.Column<double>(type: "float", nullable: true),
                    Longitud = table.Column<double>(type: "float", nullable: true),
                    NotasAcceso = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HorarioRegular = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaApertura = table.Column<DateOnly>(type: "date", nullable: true),
                    SuspensionMotivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SuspensionFechaInicio = table.Column<DateOnly>(type: "date", nullable: true),
                    SuspensionFechaFin = table.Column<DateOnly>(type: "date", nullable: true),
                    SuspensionInstrumentoLegalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocalidadAlternaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CierreFecha = table.Column<DateOnly>(type: "date", nullable: true),
                    CierreMotivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CierreInstrumentoLegalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocalidadSucesoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiciosAfectados = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JustificacionDuplicado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Localidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Localidades_DistritosMunicipales_DistritoMunicipalId",
                        column: x => x.DistritoMunicipalId,
                        principalSchema: "Organization",
                        principalTable: "DistritosMunicipales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_EstadosLocalidad_EstadoLocalidadId",
                        column: x => x.EstadoLocalidadId,
                        principalSchema: "Organization",
                        principalTable: "EstadosLocalidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_LegalInstruments_CierreInstrumentoLegalId",
                        column: x => x.CierreInstrumentoLegalId,
                        principalSchema: "Organization",
                        principalTable: "LegalInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_LegalInstruments_SuspensionInstrumentoLegalId",
                        column: x => x.SuspensionInstrumentoLegalId,
                        principalSchema: "Organization",
                        principalTable: "LegalInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_Localidades_LocalidadAlternaId",
                        column: x => x.LocalidadAlternaId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_Localidades_LocalidadPadreId",
                        column: x => x.LocalidadPadreId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_Localidades_LocalidadSucesoraId",
                        column: x => x.LocalidadSucesoraId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_Municipios_MunicipioId",
                        column: x => x.MunicipioId,
                        principalSchema: "Organization",
                        principalTable: "Municipios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_NaturalezasAdministrativas_NaturalezaAdministrativaId",
                        column: x => x.NaturalezaAdministrativaId,
                        principalSchema: "Organization",
                        principalTable: "NaturalezasAdministrativas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_OrganizationalStructures_UnidadResponsableId",
                        column: x => x.UnidadResponsableId,
                        principalSchema: "Organization",
                        principalTable: "OrganizationalStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_OrganizationalStructures_UnidadVinculadaId",
                        column: x => x.UnidadVinculadaId,
                        principalSchema: "Organization",
                        principalTable: "OrganizationalStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_Paises_PaisId",
                        column: x => x.PaisId,
                        principalSchema: "Organization",
                        principalTable: "Paises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_Provincias_ProvinciaId",
                        column: x => x.ProvinciaId,
                        principalSchema: "Organization",
                        principalTable: "Provincias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_Regiones_RegionId",
                        column: x => x.RegionId,
                        principalSchema: "Organization",
                        principalTable: "Regiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localidades_TiposLocalidad_TipoLocalidadId",
                        column: x => x.TipoLocalidadId,
                        principalSchema: "Organization",
                        principalTable: "TiposLocalidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadAuditorias",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    InstrumentoLegalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadAuditorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadAuditorias_LegalInstruments_InstrumentoLegalId",
                        column: x => x.InstrumentoLegalId,
                        principalSchema: "Organization",
                        principalTable: "LegalInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadAuditorias_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadCoberturas",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProvinciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MunicipioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DistritoMunicipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadCoberturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadCoberturas_DistritosMunicipales_DistritoMunicipalId",
                        column: x => x.DistritoMunicipalId,
                        principalSchema: "Organization",
                        principalTable: "DistritosMunicipales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadCoberturas_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadCoberturas_Municipios_MunicipioId",
                        column: x => x.MunicipioId,
                        principalSchema: "Organization",
                        principalTable: "Municipios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadCoberturas_Provincias_ProvinciaId",
                        column: x => x.ProvinciaId,
                        principalSchema: "Organization",
                        principalTable: "Provincias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadCoberturas_Regiones_RegionId",
                        column: x => x.RegionId,
                        principalSchema: "Organization",
                        principalTable: "Regiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadDocumentos",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoDocumento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    RegistradoPor = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadDocumentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadDocumentos_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadHorariosExcepcion",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: true),
                    Horario = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadHorariosExcepcion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadHorariosExcepcion_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadInstrumentosLegales",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentoLegalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Proposito = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadInstrumentosLegales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadInstrumentosLegales_LegalInstruments_InstrumentoLegalId",
                        column: x => x.InstrumentoLegalId,
                        principalSchema: "Organization",
                        principalTable: "LegalInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadInstrumentosLegales_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadResponsables",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Principal = table.Column<bool>(type: "bit", nullable: false),
                    FechaAsignacion = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadResponsables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadResponsables_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadResponsables_Responsables_ResponsableId",
                        column: x => x.ResponsableId,
                        principalSchema: "Organization",
                        principalTable: "Responsables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadServicios",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Habilitado = table.Column<bool>(type: "bit", nullable: false),
                    FechaHabilitacion = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadServicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadServicios_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadServicios_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalSchema: "Organization",
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalidadUnidadesPresencia",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LocalidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadOrganizativaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalidadUnidadesPresencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalidadUnidadesPresencia_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalSchema: "Organization",
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalidadUnidadesPresencia_OrganizationalStructures_UnidadOrganizativaId",
                        column: x => x.UnidadOrganizativaId,
                        principalSchema: "Organization",
                        principalTable: "OrganizationalStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DistritosMunicipales_Codigo",
                schema: "Organization",
                table: "DistritosMunicipales",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistritosMunicipales_MunicipioId",
                schema: "Organization",
                table: "DistritosMunicipales",
                column: "MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_EstadosLocalidad_Codigo",
                schema: "Organization",
                table: "EstadosLocalidad",
                column: "Codigo",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EstadosLocalidad_DeletedAt",
                schema: "Organization",
                table: "EstadosLocalidad",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadAuditorias_InstrumentoLegalId",
                schema: "Organization",
                table: "LocalidadAuditorias",
                column: "InstrumentoLegalId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadAuditorias_LocalidadId",
                schema: "Organization",
                table: "LocalidadAuditorias",
                column: "LocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadCoberturas_DistritoMunicipalId",
                schema: "Organization",
                table: "LocalidadCoberturas",
                column: "DistritoMunicipalId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadCoberturas_LocalidadId",
                schema: "Organization",
                table: "LocalidadCoberturas",
                column: "LocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadCoberturas_MunicipioId",
                schema: "Organization",
                table: "LocalidadCoberturas",
                column: "MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadCoberturas_ProvinciaId",
                schema: "Organization",
                table: "LocalidadCoberturas",
                column: "ProvinciaId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadCoberturas_RegionId",
                schema: "Organization",
                table: "LocalidadCoberturas",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadDocumentos_DeletedAt",
                schema: "Organization",
                table: "LocalidadDocumentos",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadDocumentos_LocalidadId",
                schema: "Organization",
                table: "LocalidadDocumentos",
                column: "LocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_CierreInstrumentoLegalId",
                schema: "Organization",
                table: "Localidades",
                column: "CierreInstrumentoLegalId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_DeletedAt",
                schema: "Organization",
                table: "Localidades",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_DistritoMunicipalId",
                schema: "Organization",
                table: "Localidades",
                column: "DistritoMunicipalId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_EstadoLocalidadId",
                schema: "Organization",
                table: "Localidades",
                column: "EstadoLocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_LocalidadAlternaId",
                schema: "Organization",
                table: "Localidades",
                column: "LocalidadAlternaId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_LocalidadPadreId",
                schema: "Organization",
                table: "Localidades",
                column: "LocalidadPadreId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_LocalidadSucesoraId",
                schema: "Organization",
                table: "Localidades",
                column: "LocalidadSucesoraId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_MunicipioId",
                schema: "Organization",
                table: "Localidades",
                column: "MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_NaturalezaAdministrativaId",
                schema: "Organization",
                table: "Localidades",
                column: "NaturalezaAdministrativaId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_OrganizationId_Codigo",
                schema: "Organization",
                table: "Localidades",
                columns: new[] { "OrganizationId", "Codigo" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_PaisId",
                schema: "Organization",
                table: "Localidades",
                column: "PaisId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_ProvinciaId",
                schema: "Organization",
                table: "Localidades",
                column: "ProvinciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_RegionId",
                schema: "Organization",
                table: "Localidades",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_SuspensionInstrumentoLegalId",
                schema: "Organization",
                table: "Localidades",
                column: "SuspensionInstrumentoLegalId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_TipoLocalidadId",
                schema: "Organization",
                table: "Localidades",
                column: "TipoLocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_UnidadResponsableId",
                schema: "Organization",
                table: "Localidades",
                column: "UnidadResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_UnidadVinculadaId",
                schema: "Organization",
                table: "Localidades",
                column: "UnidadVinculadaId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadHorariosExcepcion_LocalidadId",
                schema: "Organization",
                table: "LocalidadHorariosExcepcion",
                column: "LocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadInstrumentosLegales_InstrumentoLegalId",
                schema: "Organization",
                table: "LocalidadInstrumentosLegales",
                column: "InstrumentoLegalId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadInstrumentosLegales_LocalidadId_InstrumentoLegalId_Proposito",
                schema: "Organization",
                table: "LocalidadInstrumentosLegales",
                columns: new[] { "LocalidadId", "InstrumentoLegalId", "Proposito" },
                unique: true,
                filter: "[Proposito] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadResponsables_DeletedAt",
                schema: "Organization",
                table: "LocalidadResponsables",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadResponsables_LocalidadId",
                schema: "Organization",
                table: "LocalidadResponsables",
                column: "LocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadResponsables_ResponsableId",
                schema: "Organization",
                table: "LocalidadResponsables",
                column: "ResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadServicios_DeletedAt",
                schema: "Organization",
                table: "LocalidadServicios",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadServicios_LocalidadId_ServicioId",
                schema: "Organization",
                table: "LocalidadServicios",
                columns: new[] { "LocalidadId", "ServicioId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadServicios_ServicioId",
                schema: "Organization",
                table: "LocalidadServicios",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadUnidadesPresencia_LocalidadId_UnidadOrganizativaId",
                schema: "Organization",
                table: "LocalidadUnidadesPresencia",
                columns: new[] { "LocalidadId", "UnidadOrganizativaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalidadUnidadesPresencia_UnidadOrganizativaId",
                schema: "Organization",
                table: "LocalidadUnidadesPresencia",
                column: "UnidadOrganizativaId");

            migrationBuilder.CreateIndex(
                name: "IX_Municipios_Codigo",
                schema: "Organization",
                table: "Municipios",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Municipios_ProvinciaId",
                schema: "Organization",
                table: "Municipios",
                column: "ProvinciaId");

            migrationBuilder.CreateIndex(
                name: "IX_NaturalezasAdministrativas_Codigo",
                schema: "Organization",
                table: "NaturalezasAdministrativas",
                column: "Codigo",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NaturalezasAdministrativas_DeletedAt",
                schema: "Organization",
                table: "NaturalezasAdministrativas",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Paises_Codigo",
                schema: "Organization",
                table: "Paises",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Provincias_Codigo",
                schema: "Organization",
                table: "Provincias",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Provincias_PaisId",
                schema: "Organization",
                table: "Provincias",
                column: "PaisId");

            migrationBuilder.CreateIndex(
                name: "IX_Provincias_RegionId",
                schema: "Organization",
                table: "Provincias",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Regiones_Codigo",
                schema: "Organization",
                table: "Regiones",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Regiones_PaisId",
                schema: "Organization",
                table: "Regiones",
                column: "PaisId");

            migrationBuilder.CreateIndex(
                name: "IX_Responsables_DeletedAt",
                schema: "Organization",
                table: "Responsables",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Responsables_OrganizationId",
                schema: "Organization",
                table: "Responsables",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_DeletedAt",
                schema: "Organization",
                table: "Servicios",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_OrganizationId_Nombre",
                schema: "Organization",
                table: "Servicios",
                columns: new[] { "OrganizationId", "Nombre" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TiposLocalidad_Codigo",
                schema: "Organization",
                table: "TiposLocalidad",
                column: "Codigo",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TiposLocalidad_DeletedAt",
                schema: "Organization",
                table: "TiposLocalidad",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalidadAuditorias",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadCoberturas",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadDocumentos",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadHorariosExcepcion",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadInstrumentosLegales",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadResponsables",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadServicios",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LocalidadUnidadesPresencia",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "Responsables",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "Servicios",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "Localidades",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "DistritosMunicipales",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "EstadosLocalidad",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "NaturalezasAdministrativas",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "TiposLocalidad",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "Municipios",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "Provincias",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "Regiones",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "Paises",
                schema: "Organization");
        }
    }
}
