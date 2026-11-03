using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.Localidad;

// ── Sub-elementos de lectura ──
public record LocalidadServicioDto
{
    public Guid Id { get; init; }
    public Guid ServicioId { get; init; }
    public string? ServicioNombre { get; init; }
    public bool Habilitado { get; init; }
    public DateOnly? FechaHabilitacion { get; init; }
}

/// <summary>RF-LO-45: función de la localidad (detalle). El listado lleva solo los códigos.</summary>
public record LocalidadFuncionDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public string Categoria { get; init; } = string.Empty;
}

public record LocalidadResponsableDto
{
    public Guid Id { get; init; }
    public Guid ResponsableId { get; init; }
    public string? ResponsableNombre { get; init; }
    public string? Rol { get; init; }
    public bool Principal { get; init; }
    public DateOnly? FechaAsignacion { get; init; }

    /// <summary>RF-LO-38: insignia "Gestiona calendario" en el detalle de la localidad.</summary>
    public bool GestionaCalendario { get; init; }
}

public record LocalidadDocumentoDto
{
    public Guid Id { get; init; }
    public required string TipoDocumento { get; init; }
    public string? Descripcion { get; init; }
    public required string Url { get; init; }
    public DateTime FechaRegistro { get; init; }
    public string? RegistradoPor { get; init; }
}

public record LocalidadUnidadPresenciaDto
{
    public Guid Id { get; init; }
    public Guid UnidadOrganizativaId { get; init; }
    public string? UnidadNombre { get; init; }
    public string? Notas { get; init; }
}

public record LocalidadHorarioExcepcionDto
{
    public Guid Id { get; init; }
    public required string Descripcion { get; init; }
    public DateOnly FechaInicio { get; init; }
    public DateOnly? FechaFin { get; init; }
    public string? Horario { get; init; }
}

public record LocalidadCoberturaDto
{
    public Guid Id { get; init; }
    public Guid? RegionId { get; init; }
    public string? RegionNombre { get; init; }
    public Guid? ProvinciaId { get; init; }
    public string? ProvinciaNombre { get; init; }
    public Guid? MunicipioId { get; init; }
    public string? MunicipioNombre { get; init; }
    public Guid? DistritoMunicipalId { get; init; }
    public string? DistritoMunicipalNombre { get; init; }
}

public record LocalidadInstrumentoLegalDto
{
    public Guid Id { get; init; }
    public Guid InstrumentoLegalId { get; init; }
    public string? Proposito { get; init; }
    public string? Numero { get; init; }
    public LegalInstrumentType? Tipo { get; init; }
    public DateOnly? Fecha { get; init; }
    public string? DocumentUrl { get; init; }
}

public record LocalidadAuditoriaDto
{
    public Guid Id { get; init; }
    public LocalidadAccion Accion { get; init; }
    public string? Motivo { get; init; }
    public string? Usuario { get; init; }
    public DateTime Fecha { get; init; }
    public Guid? InstrumentoLegalId { get; init; }
    public string? Detalle { get; init; }
    /// <summary>RF-LO-44: canal del cambio; nulo en filas anteriores al calendario laboral.</summary>
    public CanalCambio? Canal { get; init; }
}

/// <summary>Fila ligera para la lista de localidades (RF-LO-15).</summary>
public record LocalidadListItemDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string NombreOficial { get; init; }
    public Guid TipoLocalidadId { get; init; }
    public string? TipoNombre { get; init; }
    public Guid EstadoLocalidadId { get; init; }
    public string? EstadoNombre { get; init; }
    public string? EstadoColor { get; init; }
    public Guid? ProvinciaId { get; init; }
    public string? ProvinciaNombre { get; init; }
    public Guid? MunicipioId { get; init; }
    public string? MunicipioNombre { get; init; }
    public double? Latitud { get; init; }
    public double? Longitud { get; init; }

    // RF-LO-19 / RF-LO-45 (campos aditivos para integraciones y la UI).
    public Guid PaisId { get; init; }
    public string? PaisCodigo { get; init; }
    public string? PaisNombre { get; init; }
    public string? Ciudad { get; init; }
    public string? ZonaHoraria { get; init; }
    public string? EstadoCodigo { get; init; }

    /// <summary>Códigos de las funciones de la localidad (p. ej. EMBAJADA, SECCION_CONSULAR), ordenados.</summary>
    public IReadOnlyList<string> Funciones { get; init; } = [];
}

/// <summary>Carga ligera para el mapa institucional (RF-LO-16): solo localidades con coordenadas.</summary>
public record LocalidadMapItemDto
{
    public Guid Id { get; init; }
    public required string NombreOficial { get; init; }
    public required string Codigo { get; init; }
    public double Latitud { get; init; }
    public double Longitud { get; init; }
    public Guid TipoLocalidadId { get; init; }
    public string? TipoNombre { get; init; }
    public string? TipoColor { get; init; }
    public Guid EstadoLocalidadId { get; init; }
    public string? EstadoNombre { get; init; }
    public string? EstadoColor { get; init; }
    public Guid? UnidadResponsableId { get; init; }
    public string? UnidadResponsableNombre { get; init; }
    public string? DireccionCompleta { get; init; }
}

/// <summary>Detalle completo de una localidad (RF-LO-14).</summary>
public record LocalidadDetailDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string NombreOficial { get; init; }
    public string? NombreCorto { get; init; }
    public string? Sigla { get; init; }

    public Guid TipoLocalidadId { get; init; }
    public string? TipoNombre { get; init; }
    public Guid NaturalezaAdministrativaId { get; init; }
    public string? NaturalezaNombre { get; init; }
    public Guid EstadoLocalidadId { get; init; }
    public string? EstadoNombre { get; init; }
    public string? EstadoColor { get; init; }

    public Guid? UnidadResponsableId { get; init; }
    public string? UnidadResponsableNombre { get; init; }
    public Guid? UnidadVinculadaId { get; init; }
    public string? UnidadVinculadaNombre { get; init; }

    public Guid? LocalidadPadreId { get; init; }
    public string? LocalidadPadreNombre { get; init; }

    public Guid PaisId { get; init; }
    public string? PaisNombre { get; init; }
    public Guid? RegionId { get; init; }
    public string? RegionNombre { get; init; }
    public Guid? ProvinciaId { get; init; }
    public string? ProvinciaNombre { get; init; }
    public Guid? MunicipioId { get; init; }
    public string? MunicipioNombre { get; init; }
    public Guid? DistritoMunicipalId { get; init; }
    public string? DistritoMunicipalNombre { get; init; }
    public string? Ciudad { get; init; }
    public string? SubdivisionAdministrativa { get; init; }
    public string? SectorBarrioParaje { get; init; }
    public required string DireccionCompleta { get; init; }
    public string? EdificioPisoLocalReferenciaInterna { get; init; }
    public string? CodigoPostal { get; init; }

    public double? Latitud { get; init; }
    public double? Longitud { get; init; }

    public string ZonaHoraria { get; init; } = "America/Santo_Domingo";

    /// <summary>RF-LO-36: localidad de respaldo y plantilla de calendario de la organización (la UI muestra la insignia y oculta el botón).</summary>
    public bool EsLocalidadCalendarioPredeterminada { get; init; }

    /// <summary>RF-LO-43: versión del calendario efectivo de la localidad.</summary>
    public long CalendarioVersion { get; init; }

    public string? NotasAcceso { get; init; }
    public string? HorarioRegular { get; init; }
    public string? Observaciones { get; init; }

    public DateOnly? FechaApertura { get; init; }
    public string? SuspensionMotivo { get; init; }
    public DateOnly? SuspensionFechaInicio { get; init; }
    public DateOnly? SuspensionFechaFin { get; init; }
    public Guid? LocalidadAlternaId { get; init; }
    public string? LocalidadAlternaNombre { get; init; }
    public DateOnly? CierreFecha { get; init; }
    public string? CierreMotivo { get; init; }
    public Guid? LocalidadSucesoraId { get; init; }
    public string? LocalidadSucesoraNombre { get; init; }
    public string? ServiciosAfectados { get; init; }

    public string? JustificacionDuplicado { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public IEnumerable<LocalidadServicioDto> Servicios { get; init; } = [];
    public IEnumerable<LocalidadResponsableDto> Responsables { get; init; } = [];
    public IEnumerable<LocalidadDocumentoDto> Documentos { get; init; } = [];
    public IEnumerable<LocalidadUnidadPresenciaDto> UnidadesPresencia { get; init; } = [];
    public IEnumerable<LocalidadHorarioExcepcionDto> HorariosExcepcion { get; init; } = [];
    public IEnumerable<LocalidadCoberturaDto> Coberturas { get; init; } = [];
    public IEnumerable<LocalidadInstrumentoLegalDto> InstrumentosLegales { get; init; } = [];

    /// <summary>RF-LO-45: funciones asignadas (id, código, nombre, categoría), ordenadas por código.</summary>
    public IEnumerable<LocalidadFuncionDto> Funciones { get; init; } = [];
    public IEnumerable<LocalidadAuditoriaDto> Historial { get; init; } = [];
}
