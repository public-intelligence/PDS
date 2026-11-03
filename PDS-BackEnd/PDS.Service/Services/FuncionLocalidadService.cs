using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.CatalogoLocalidad;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services;

/// <summary>
/// Catálogo de funciones de localidad (RF-LO-45, B30): alta, edición, baja lógica y consulta paginada con búsqueda.
/// Contrato del frontend en <c>funciones-de-localidad.md</c>: la lista es un arreglo plano y la paginación viaja en
/// los encabezados; los errores usan <c>ErrorDto</c> con mensajes en español.
/// </summary>
public interface IFuncionLocalidadService
{
    Task<IHttpResult<PagedResponseDto<FuncionLocalidadDto>>> ListAsync(
        string? q, int pageNumber, int pageSize, HttpContext httpContext, CancellationToken ct = default);

    Task<IHttpResult<FuncionLocalidadDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<IHttpResult<FuncionLocalidadDto>> CreateAsync(FuncionLocalidadUpsertDto dto, CancellationToken ct = default);
    Task<IHttpResult<FuncionLocalidadDto>> UpdateAsync(Guid id, FuncionLocalidadUpsertDto dto, CancellationToken ct = default);
    Task<IHttpResult> DeleteAsync(Guid id, CancellationToken ct = default);
}

public class FuncionLocalidadService(
    IUnitOfWork<PDSDbContext> uow,
    IPaginationService paginationService,
    TimeProvider clock) : IFuncionLocalidadService
{
    private const int MaxCodigo = 50;
    private const int MaxNombre = 250;
    private const int MaxDescripcion = 1000;
    private const int PageSizeDefault = 20;
    private const int PageSizeMax = 100;

    private PDSDbContext Ctx => uow.Context;

    public async Task<IHttpResult<PagedResponseDto<FuncionLocalidadDto>>> ListAsync(
        string? q, int pageNumber, int pageSize, HttpContext httpContext, CancellationToken ct = default)
    {
        var query = Ctx.FuncionesLocalidad.AsNoTracking().Where(f => f.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(q))
        {
            // "Contiene" sin distinguir mayúsculas en código, nombre y descripción.
            var term = q.Trim().ToUpper();
            query = query.Where(f =>
                f.Codigo.ToUpper().Contains(term) ||
                f.Nombre.ToUpper().Contains(term) ||
                (f.Descripcion != null && f.Descripcion.ToUpper().Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var page = pageNumber < 1 ? 1 : pageNumber;
        var size = pageSize is < 1 or > PageSizeMax ? PageSizeDefault : pageSize;

        // Orden estable entre páginas: por código y, ante empate imposible por el índice único, por Id.
        var filas = await query
            .OrderBy(f => f.Codigo).ThenBy(f => f.Id)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync(ct);

        var paged = paginationService.BuildPaginationUrl(httpContext, page, size, total, filas.Select(Map).ToList());
        return HttpResult<PagedResponseDto<FuncionLocalidadDto>>.Success(paged);
    }

    public async Task<IHttpResult<FuncionLocalidadDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await Ctx.FuncionesLocalidad.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, ct);
        return entity is null ? NoEncontrada<FuncionLocalidadDto>() : HttpResult<FuncionLocalidadDto>.Success(Map(entity));
    }

    public async Task<IHttpResult<FuncionLocalidadDto>> CreateAsync(FuncionLocalidadUpsertDto dto, CancellationToken ct = default)
    {
        var invalid = Validar<FuncionLocalidadDto>(dto, out var codigo, out var nombre, out var categoria);
        if (invalid is not null) return invalid;

        var upper = codigo.ToUpper();
        if (await Ctx.FuncionesLocalidad.AnyAsync(f => f.DeletedAt == null && f.Codigo.ToUpper() == upper, ct))
            return CodigoDuplicado<FuncionLocalidadDto>(codigo);

        var ahora = clock.GetUtcNow().UtcDateTime;
        var entity = new FuncionLocalidad
        {
            Id = Guid.NewGuid(),
            Codigo = codigo,
            Nombre = nombre,
            Categoria = categoria,
            Descripcion = Normalizar(dto.Descripcion),
            CreatedAt = ahora,
            UpdatedAt = ahora
        };
        Ctx.FuncionesLocalidad.Add(entity);
        await Ctx.SaveChangesAsync(ct);
        return HttpResult<FuncionLocalidadDto>.Success(Map(entity), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<FuncionLocalidadDto>> UpdateAsync(Guid id, FuncionLocalidadUpsertDto dto, CancellationToken ct = default)
    {
        var entity = await Ctx.FuncionesLocalidad.FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, ct);
        if (entity is null) return NoEncontrada<FuncionLocalidadDto>();

        var invalid = Validar<FuncionLocalidadDto>(dto, out var codigo, out var nombre, out var categoria);
        if (invalid is not null) return invalid;

        // El código es inmutable porque las integraciones lo referencian: el PUT lo repite sin cambios.
        if (!string.Equals(codigo, entity.Codigo, StringComparison.OrdinalIgnoreCase))
            return Fallo<FuncionLocalidadDto>("Error de validación", "codigo",
                "El código de una función no se puede cambiar.", HttpStatusCode.BadRequest);

        // PUT reemplaza el registro completo: una descripción omitida queda en null (así se borra).
        entity.Nombre = nombre;
        entity.Categoria = categoria;
        entity.Descripcion = Normalizar(dto.Descripcion);
        entity.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await Ctx.SaveChangesAsync(ct);
        return HttpResult<FuncionLocalidadDto>.Success(Map(entity));
    }

    public async Task<IHttpResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await Ctx.FuncionesLocalidad.FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, ct);
        if (entity is null)
            return HttpResult.Failure("No se encontró la función de localidad",
                new Dictionary<string, IEnumerable<string>> { { "errors", ["No se encontró la función de localidad solicitada"] } },
                HttpStatusCode.NotFound);

        // En uso: alguna localidad vigente (no eliminada) tiene asignada la función.
        var enUso = await Ctx.LocalidadFunciones.AnyAsync(lf =>
            lf.FuncionLocalidadId == id && lf.Localidad != null && lf.Localidad.DeletedAt == null, ct);
        if (enUso)
            return HttpResult.Failure("Función en uso",
                new Dictionary<string, IEnumerable<string>> { { "funcion", ["No se puede eliminar: hay localidades que usan esta función."] } },
                HttpStatusCode.Conflict);

        entity.DeletedAt = clock.GetUtcNow().UtcDateTime;
        await Ctx.SaveChangesAsync(ct);
        return HttpResult.Success();
    }

    // ───────────────────────────── Utilidades ─────────────────────────────

    private static IHttpResult<T>? Validar<T>(
        FuncionLocalidadUpsertDto dto, out string codigo, out string nombre, out CategoriaFuncionLocalidad categoria)
    {
        codigo = dto.Codigo?.Trim() ?? string.Empty;
        nombre = dto.Nombre?.Trim() ?? string.Empty;
        categoria = default;

        var errores = new Dictionary<string, IEnumerable<string>>();
        if (codigo.Length == 0) errores["codigo"] = ["El código es obligatorio."];
        else if (codigo.Length > MaxCodigo) errores["codigo"] = [$"El código no puede superar {MaxCodigo} caracteres."];

        if (nombre.Length == 0) errores["nombre"] = ["El nombre es obligatorio."];
        else if (nombre.Length > MaxNombre) errores["nombre"] = [$"El nombre no puede superar {MaxNombre} caracteres."];

        if (string.IsNullOrWhiteSpace(dto.Categoria))
            errores["categoria"] = ["La categoría es obligatoria."];
        else if (!TryParseCategoria(dto.Categoria, out categoria))
            errores["categoria"] = ["La categoría debe ser Diplomatica, Consular o Multilateral."];

        if (dto.Descripcion is { Length: > MaxDescripcion })
            errores["descripcion"] = [$"La descripción no puede superar {MaxDescripcion} caracteres."];

        return errores.Count == 0
            ? null
            : HttpResult<T>.Failure("Error de validación", errores, HttpStatusCode.BadRequest);
    }

    // Solo nombres del enum (sin números sueltos), sin distinguir mayúsculas.
    private static bool TryParseCategoria(string valor, out CategoriaFuncionLocalidad categoria)
    {
        categoria = default;
        var texto = valor.Trim();
        return Enum.GetNames<CategoriaFuncionLocalidad>().Any(n => string.Equals(n, texto, StringComparison.OrdinalIgnoreCase))
               && Enum.TryParse(texto, ignoreCase: true, out categoria);
    }

    private static string? Normalizar(string? descripcion) =>
        string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();

    private static FuncionLocalidadDto Map(FuncionLocalidad f) => new()
    {
        Id = f.Id,
        Codigo = f.Codigo,
        Nombre = f.Nombre,
        Categoria = f.Categoria,
        Descripcion = f.Descripcion
    };

    private static IHttpResult<T> NoEncontrada<T>() =>
        HttpResult<T>.Failure("No se encontró la función de localidad",
            new Dictionary<string, IEnumerable<string>> { { "errors", ["No se encontró la función de localidad solicitada"] } },
            HttpStatusCode.NotFound);

    private static IHttpResult<T> CodigoDuplicado<T>(string codigo) =>
        Fallo<T>("Error de validación", "codigo", $"Ya existe una función con el código {codigo}.", HttpStatusCode.Conflict);

    private static IHttpResult<T> Fallo<T>(string title, string key, string msg, HttpStatusCode code) =>
        HttpResult<T>.Failure(title, new Dictionary<string, IEnumerable<string>> { { key, [msg] } }, code);
}
