using Microsoft.AspNetCore.Mvc;

namespace PDS.Domain.DTOs.Localidad;

/// <summary>Suspensión temporal de una localidad (RF-LO-09).</summary>
public record SuspenderLocalidadDto
{
    public required string Motivo { get; init; }
    public DateOnly? FechaInicio { get; init; }
    public DateOnly? FechaFin { get; init; }
    public Guid? InstrumentoLegalId { get; init; }
    public Guid? LocalidadAlternaId { get; init; }
}

/// <summary>Reactivación (levantamiento de suspensión) de una localidad.</summary>
public record ReactivarLocalidadDto
{
    public string? Motivo { get; init; }
}

/// <summary>Cierre / desactivación de una localidad (RF-LO-10). No elimina datos históricos.</summary>
public record CerrarLocalidadDto
{
    public required string Motivo { get; init; }
    public DateOnly? CierreFecha { get; init; }
    public Guid? InstrumentoLegalId { get; init; }
    public Guid? LocalidadSucesoraId { get; init; }
    public string? ServiciosAfectados { get; init; }
}

// ── Validación de duplicados (RF-LO-13) ──
public record DuplicateWarningDto
{
    public required string Campo { get; init; }
    public Guid LocalidadId { get; init; }
    public required string Codigo { get; init; }
    public required string NombreOficial { get; init; }
    public required string Motivo { get; init; }
}

public record DuplicateCheckResultDto
{
    public bool HasWarnings { get; init; }
    public IEnumerable<DuplicateWarningDto> Warnings { get; init; } = [];
}

public class DuplicateCheckRequestDto
{
    [FromQuery(Name = "codigo")] public string? Codigo { get; init; }
    [FromQuery(Name = "nombreOficial")] public string? NombreOficial { get; init; }
    [FromQuery(Name = "direccionCompleta")] public string? DireccionCompleta { get; init; }
    [FromQuery(Name = "latitud")] public double? Latitud { get; init; }
    [FromQuery(Name = "longitud")] public double? Longitud { get; init; }
    [FromQuery(Name = "paisId")] public Guid? PaisId { get; init; }
    [FromQuery(Name = "ciudad")] public string? Ciudad { get; init; }
    [FromQuery(Name = "provinciaId")] public Guid? ProvinciaId { get; init; }
    [FromQuery(Name = "municipioId")] public Guid? MunicipioId { get; init; }
    [FromQuery(Name = "tipoLocalidadId")] public Guid? TipoLocalidadId { get; init; }
    [FromQuery(Name = "excludeId")] public Guid? ExcludeId { get; init; }
}

/// <summary>Filtros de búsqueda y paginación de localidades (RF-LO-15 / RF-LO-16).</summary>
public class LocalidadFilterDto
{
    [FromQuery(Name = "nombre")] public string? Nombre { get; init; }
    [FromQuery(Name = "codigo")] public string? Codigo { get; init; }
    [FromQuery(Name = "tipoId")] public Guid? TipoId { get; init; }
    [FromQuery(Name = "estadoId")] public Guid? EstadoId { get; init; }
    [FromQuery(Name = "naturalezaId")] public Guid? NaturalezaId { get; init; }
    [FromQuery(Name = "regionId")] public Guid? RegionId { get; init; }
    [FromQuery(Name = "provinciaId")] public Guid? ProvinciaId { get; init; }
    [FromQuery(Name = "municipioId")] public Guid? MunicipioId { get; init; }
    [FromQuery(Name = "unidadId")] public Guid? UnidadId { get; init; }
    [FromQuery(Name = "servicioId")] public Guid? ServicioId { get; init; }
    [FromQuery(Name = "instrumentoLegalId")] public Guid? InstrumentoLegalId { get; init; }

    // RF-LO-19 / RF-LO-45: filtros por país, estado y función. Los de código (alfa-2, código de estado y de función) son estables
    // entre ambientes: las integraciones no deben incrustar GUIDs.
    [FromQuery(Name = "paisId")] public Guid? PaisId { get; init; }
    [FromQuery(Name = "paisCodigo")] public string? PaisCodigo { get; init; }
    [FromQuery(Name = "estadoCodigo")] public string? EstadoCodigo { get; init; }
    [FromQuery(Name = "funcionId")] public Guid? FuncionId { get; init; }
    [FromQuery(Name = "funcionCodigo")] public string? FuncionCodigo { get; init; }

    [FromQuery(Name = "pageNumber")] public int PageNumber { get; init; } = 1;
    [FromQuery(Name = "pageSize")] public int PageSize { get; init; } = 20;
}
