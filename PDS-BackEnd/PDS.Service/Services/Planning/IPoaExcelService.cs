using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

/// <summary>
/// Excel del POA (M6): plantilla con las columnas personalizadas de la plantilla del ciclo,
/// exportación (por plan e institucional por ciclo) e importación en dos fases con el mismo
/// contrato funcional del CSV del mock (una fila por unidad de medida, filas con el mismo
/// Código = un producto, conservación de ids de productos/unidades/tareas por código/nombre).
/// </summary>
public interface IPoaExcelService
{
    Task<IHttpResult<ExcelFileDto>> BuildTemplateAsync(int organizationId, Guid planId);
    Task<IHttpResult<ExcelFileDto>> ExportPlanAsync(int organizationId, Guid planId);

    /// <summary>Todas las áreas del ciclo, con la columna "Área" al frente.</summary>
    Task<IHttpResult<ExcelFileDto>> ExportCycleAsync(int organizationId, Guid cycleId);

    Task<IHttpResult<ImportResultDto>> ImportAsync(
        int organizationId, Guid planId, Stream file, string mode, bool dryRun, Guid userId, string? userName);
}
