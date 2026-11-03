using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

/// <summary>
/// Excel del PEI (M3): plantilla descargable con columnas de años dinámicas del período,
/// exportación del contenido, e importación en dos fases (dryRun valida y reporta; sin
/// errores y dryRun=false aplica en transacción). Solo la enmienda Draft admite importación.
/// </summary>
public interface IStrategicPlanExcelService
{
    /// <summary>Plantilla vacía (hoja "PEI" + hoja "Catálogos" con las áreas del organigrama).</summary>
    Task<IHttpResult<ExcelFileDto>> BuildTemplateAsync(int organizationId, Guid planId);

    /// <summary>Exporta el contenido de la versión (una fila por unidad de medida).</summary>
    Task<IHttpResult<ExcelFileDto>> ExportAsync(int organizationId, Guid planId);

    /// <summary>
    /// Importa el archivo sobre el Draft. Modo "actualizar" conserva lo no incluido;
    /// "reemplazar" elimina lo ausente sin historia (lo con historia se conserva con aviso).
    /// Las metas de años transcurridos de unidades existentes deben coincidir con lo guardado.
    /// </summary>
    Task<IHttpResult<ImportResultDto>> ImportAsync(
        int organizationId, Guid planId, Stream file, string mode, bool dryRun, Guid userId, string? userName);
}

/// <summary>Archivo generado en memoria listo para descargar.</summary>
public record ExcelFileDto
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public string ContentType { get; init; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
