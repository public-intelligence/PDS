using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.OrganizationalStructure;
using PDS.Domain.DTOs.OrganizationalStructureAreatype;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services;

public class OrganizationalStructureService(
    ILogger<OrganizationalStructureService> logger,
    IMapper mapper,
    IRepository<OrganizationalStructure> organizationalStructureRepository,
    IRepository<Position> positionRepository,
    IPaginationService paginationService,
    IRepository<StructureVersion> versionRepository,
    IRepository<OrganizationalStructureAreaType> areaTypeRepository,
    IUnitOfWork<PDSDbContext> unitOfWork,
    IConfiguration configuration
    ) : IOrganizationalStructureService
{
    private readonly ILogger<OrganizationalStructureService> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly IRepository<OrganizationalStructure> _organizationalStructureRepository = organizationalStructureRepository;
    private readonly IRepository<Position> _positionRepository = positionRepository;
    private readonly IRepository<StructureVersion> _versionRepository = versionRepository;
    private readonly IRepository<OrganizationalStructureAreaType> _areaTypeRepository = areaTypeRepository;
    private readonly IUnitOfWork<PDSDbContext> _uow = unitOfWork;
    private readonly IPaginationService _paginationService = paginationService;
    private readonly IConfiguration _configuration = configuration;

    private const int MaxImportDepth = 10;

    /// <summary>
    /// La estructura operativa solo es editable cuando existe un borrador de versión en curso.
    /// Mientras no haya borrador, la estructura vigente queda congelada (regla de negocio RF-FA).
    /// </summary>
    private async Task<bool> HasOpenDraftAsync(int organizationId)
    {
        var result = await _versionRepository.AnyAsync(v =>
            v.OrganizationId == organizationId && v.Status == VersionStatus.Draft);
        return result.IsSuccess && result.Value;
    }

    private static readonly string[] NoDraftError = ["Debe crear un borrador de versión antes de modificar la estructura."];

    public async Task<IHttpResult<OrganizationalStructureDto>> CreateAsync(int organizationId, Guid requestedById, CreateOrganizationalStructureDto createOrganizationalStructureDto)
    {
        if (!await HasOpenDraftAsync(organizationId))
        {
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", NoDraftError } };
            return HttpResult<OrganizationalStructureDto>.Failure("No se pudo modificar la estructura organizacional", detail, HttpStatusCode.Conflict);
        }

        var existingStructureNameResult = await _organizationalStructureRepository.GetAsync(os => os.OrganizationId == organizationId && os.Name.ToLower().Trim() == createOrganizationalStructureDto.Name.ToLower().Trim());
        var details = new Dictionary<string, IEnumerable<string>>();
        if (!existingStructureNameResult.IsSuccess)
        {
            var title = "Error al crear la estructura organizacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar la existencia del nombre de la estructura organizacional"] } };
            return HttpResult<OrganizationalStructureDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var existingStructureName = existingStructureNameResult.Value;
        if (existingStructureName != null)
        {
            details.TryAdd("name", ["El nombre ya esta registrado"]);
        }

        //var organizationResult = await _legalEntityService.GetOrganizationByIdAsync(organizationId);
        //if (!organizationResult.IsSuccess)
        //{
        //    var title = "Error al crear la estructura organizacional";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo recuperar la organización"] } };
        //    return HttpResult<OrganizationalStructureDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}

        string newCode;

        if (createOrganizationalStructureDto.ParentId.HasValue)
        {
            var parentResult = await _organizationalStructureRepository.GetAsync(os =>
            os.OrganizationId == organizationId
            &&
                os.Id == createOrganizationalStructureDto.ParentId.Value
            );
            if (!parentResult.IsSuccess)
            {
                _logger.LogWarning(
                    $"Estructura organizacional padre con ID {createOrganizationalStructureDto.ParentId} no encontrada."
                );
                var title = "Error al crear la estructura organizacional";
                var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo recuperar la estructura organizacional padre"] } };
                return HttpResult<OrganizationalStructureDto>.Failure(title, detail, HttpStatusCode.BadRequest);
            }
            var parent = parentResult.Value;
            if (parent is null)
            {
                details.TryAdd("parentId", ["La estructura organizacional padre no existe"]);
            }
            if (details.Count > 0)
            {
                var title = "No se pudo crear la estructura organizacional";
                return HttpResult<OrganizationalStructureDto>.Failure(title, details, HttpStatusCode.BadRequest);
            }

            var parentHasCycle = await HasCycleInAncestryAsync(organizationId, createOrganizationalStructureDto.ParentId.Value);
            if (parentHasCycle)
            {
                var title = "No se pudo crear la estructura organizacional";
                var detail = new Dictionary<string, IEnumerable<string>> { { "parentId", ["No se puede asignar una estructura padre con jerarquía cíclica"] } };
                return HttpResult<OrganizationalStructureDto>.Failure(title, detail, HttpStatusCode.BadRequest);
            }

            var parentCode = parent.Code;

            var childCountResult = await _organizationalStructureRepository.CountAsync(os =>
                os.OrganizationId == organizationId && os.ParentId == createOrganizationalStructureDto.ParentId.Value,
                q => q,
                true
            );
            var chiltCount = childCountResult.Value;

            newCode = $"{parentCode}-{chiltCount + 1:D2}";
        }
        else
        {
            var rootCountResult = await _organizationalStructureRepository.CountAsync(os =>
                os.OrganizationId == organizationId && os.ParentId == null,
                q => q,
                true
            );
            var rootCount = rootCountResult.Value;
            newCode = $"{rootCount + 1:D2}";
        }

        var organizationalStructure = _mapper.Map<OrganizationalStructure>(createOrganizationalStructureDto);
        organizationalStructure.OrganizationId = organizationId;
        organizationalStructure.Code = newCode;

        var createResult = await _organizationalStructureRepository.CreateAsync(organizationalStructure);
        if (!createResult.IsSuccess)
        {
            _logger.LogError(
                $"No se pudo crear la estructura organizacional."
            );
            var title = "Error al crear la estructura organizacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo crear la estructura organizacional"] } };
            return HttpResult<OrganizationalStructureDto>.Failure(
                title, detail, HttpStatusCode.BadRequest
            );

        }
        var getStructure = await _organizationalStructureRepository.GetAsync(
            os => os.OrganizationId == organizationId && os.Id == createResult.Value.Id,
            query =>
                query
                    .Include(os => os.AreaType)
                    .Include(os => os.Positions.Where(p => p.DeletedAt == null))
                        .ThenInclude(p => p.OccupationalGroup)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null))
                        .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null))
                            .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null))
                            .ThenInclude(d2 => d2.Dependencies.Where(d => d.DeletedAt == null))
                                .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null))
                            .ThenInclude(d2 => d2.Dependencies.Where(d => d.DeletedAt == null))
                                .ThenInclude(d3 => d3.Dependencies.Where(d => d.DeletedAt == null))
                                    .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null))
                            .ThenInclude(d2 => d2.Dependencies.Where(d => d.DeletedAt == null))
                                .ThenInclude(d3 => d3.Dependencies.Where(d => d.DeletedAt == null))
                                    .ThenInclude(d4 => d4.Dependencies.Where(d => d.DeletedAt == null))
                                        .ThenInclude(d => d.AreaType)
        );

        if (!getStructure.IsSuccess)
        {
            var title = "Error al crear la estructura organizacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo recuperar la estructura organizacional creada"] } };

            return HttpResult<OrganizationalStructureDto>.Failure(
                title, detail, HttpStatusCode.BadRequest
            );
        }
        if (details.Count > 0)
        {
            var title = "No se pudo crear la estructura organizacional";
            return HttpResult<OrganizationalStructureDto>.Failure(title, details, HttpStatusCode.BadRequest);
        }
        var organizationalStructureDto = _mapper.Map<OrganizationalStructureDto>(getStructure.Value);

        return HttpResult<OrganizationalStructureDto>.Success(organizationalStructureDto);
    }

    /// <summary>
    /// Importa una estructura organizacional completa (árbol) desde una plantilla JSON, de forma
    /// atómica y dentro del borrador en curso. Solo agrega unidades nuevas: los códigos e
    /// identificadores se regeneran en el servidor; el tipo de área se resuelve por nombre (o por
    /// id explícito). Valida el formato, la existencia de los tipos de área y la unicidad de
    /// nombres antes de escribir nada.
    /// </summary>
    public async Task<IHttpResult<IEnumerable<OrganizationalStructureDto>>> ImportAsync(
        int organizationId,
        Guid requestedById,
        ImportStructureDto importStructureDto)
    {
        // 1. La estructura solo es editable con un borrador en curso (misma regla que el alta manual).
        if (!await HasOpenDraftAsync(organizationId))
        {
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", NoDraftError } };
            return HttpResult<IEnumerable<OrganizationalStructureDto>>.Failure(
                "No se pudo importar la estructura organizacional", detail, HttpStatusCode.Conflict);
        }

        var nodes = importStructureDto?.Nodes ?? [];

        // 2. Validación de formato (autoritativa, independiente del cliente).
        var formatErrors = new List<string>();
        if (nodes.Count == 0)
        {
            formatErrors.Add("El archivo no contiene unidades para importar ('nodes' está vacío).");
        }
        ValidateImportNodes(nodes, depth: 1, path: "nodes", errors: formatErrors);
        if (formatErrors.Count > 0)
        {
            var detail = new Dictionary<string, IEnumerable<string>> { { "format", formatErrors } };
            return HttpResult<IEnumerable<OrganizationalStructureDto>>.Failure(
                "El formato del archivo no es válido", detail, HttpStatusCode.BadRequest);
        }

        // 3. Resolver tipos de área contra el catálogo global (por id explícito o por nombre).
        var areaTypesResult = await _areaTypeRepository.GetAllAsync();
        if (!areaTypesResult.IsSuccess || areaTypesResult.Value is null)
        {
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener el catálogo de tipos de área."] } };
            return HttpResult<IEnumerable<OrganizationalStructureDto>>.Failure(
                "Error al importar la estructura organizacional", detail, HttpStatusCode.BadRequest);
        }
        var areaTypes = areaTypesResult.Value.ToList();
        var areaTypeIds = areaTypes.Select(a => a.Id).ToHashSet();
        var areaTypeByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in areaTypes)
        {
            areaTypeByName[a.Name.Trim()] = a.Id; // El nombre es único (índice único en BD).
        }

        // 4. Unicidad de nombres: dentro del archivo y contra lo ya registrado en la organización.
        var existingResult = await _organizationalStructureRepository.FilterAsync(os => os.OrganizationId == organizationId);
        var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (existingResult.IsSuccess && existingResult.Value is not null)
        {
            foreach (var os in existingResult.Value)
            {
                existingNames.Add(os.Name.Trim());
            }
        }

        // Secuencia de códigos raíz: continúa a partir de las raíces existentes (incluye eliminadas
        // lógicamente para no reutilizar códigos, igual que CreateAsync).
        var rootCountResult = await _organizationalStructureRepository.CountAsync(
            os => os.OrganizationId == organizationId && os.ParentId == null, q => q, true);
        var rootStartSeq = rootCountResult.IsSuccess ? rootCountResult.Value : 0;

        // 5. Construir las entidades en memoria (sin consultas por nodo): se asigna un Id explícito a
        // cada nodo para poder enlazar ParentId antes de un único SaveChanges, y se generan los
        // códigos siguiendo la convención existente (raíz "NN", hijo "{códigoPadre}-NN").
        var entities = new List<OrganizationalStructure>();
        var unresolvedAreaTypes = new List<string>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateInFile = new List<string>();
        var conflictWithExisting = new List<string>();

        void BuildLevel(List<ImportStructureNodeDto> levelNodes, Guid? parentId, string? parentCode, int startSeq)
        {
            var seq = startSeq;
            foreach (var node in levelNodes)
            {
                seq++;
                var code = parentCode is null ? $"{seq:D2}" : $"{parentCode}-{seq:D2}";

                // Resolver tipo de área: id explícito válido tiene prioridad; si no, por nombre.
                Guid? areaTypeId = null;
                if (node.AreaTypeId.HasValue && areaTypeIds.Contains(node.AreaTypeId.Value))
                {
                    areaTypeId = node.AreaTypeId.Value;
                }
                else if (!string.IsNullOrWhiteSpace(node.AreaType) && areaTypeByName.TryGetValue(node.AreaType.Trim(), out var resolvedId))
                {
                    areaTypeId = resolvedId;
                }
                else
                {
                    unresolvedAreaTypes.Add(node.AreaTypeId?.ToString() ?? node.AreaType?.Trim() ?? "(sin tipo de área)");
                }

                var name = node.Name.Trim();
                if (!seenNames.Add(name)) duplicateInFile.Add(name);
                if (existingNames.Contains(name)) conflictWithExisting.Add(name);

                var id = Guid.NewGuid();
                entities.Add(new OrganizationalStructure
                {
                    Id = id,
                    OrganizationId = organizationId,
                    ParentId = parentId,
                    AreaTypeId = areaTypeId ?? Guid.Empty,
                    Code = code,
                    Name = name,
                    AreaType = null!, // Se enlaza por la FK AreaTypeId; la navegación no se materializa aquí.
                });

                if (node.Children is { Count: > 0 })
                {
                    BuildLevel(node.Children, id, code, 0);
                }
            }
        }

        BuildLevel(nodes, null, null, rootStartSeq);

        // Acumular todos los errores de negocio antes de escribir.
        var details = new Dictionary<string, IEnumerable<string>>();
        if (unresolvedAreaTypes.Count > 0)
        {
            details["areaType"] = unresolvedAreaTypes.Distinct().Select(n => $"Tipo de área no encontrado: '{n}'").ToList();
        }
        var nameErrors = new List<string>();
        nameErrors.AddRange(duplicateInFile.Distinct().Select(n => $"Nombre duplicado en el archivo: '{n}'"));
        nameErrors.AddRange(conflictWithExisting.Distinct().Select(n => $"El nombre ya está registrado: '{n}'"));
        if (nameErrors.Count > 0)
        {
            details["name"] = nameErrors;
        }
        if (details.Count > 0)
        {
            return HttpResult<IEnumerable<OrganizationalStructureDto>>.Failure(
                "No se pudo importar la estructura organizacional", details, HttpStatusCode.BadRequest);
        }

        try
        {
            _uow.Context.OrganizationalStructures.AddRange(entities);
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Descartar las entidades pendientes para que no se arrastren a otro SaveChanges del mismo contexto.
            _uow.Context.ChangeTracker.Clear();
            _logger.LogError(ex, "No se pudo importar la estructura organizacional.");
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo guardar la estructura importada."] } };
            return HttpResult<IEnumerable<OrganizationalStructureDto>>.Failure(
                "Error al importar la estructura organizacional", detail, HttpStatusCode.BadRequest);
        }

        // 7. Devolver el árbol actualizado (reusa la carga y el mapeo de ListAsync).
        return await ListAsync(organizationId);
    }

    /// <summary>
    /// Valida recursivamente el formato de la plantilla (mismas reglas que el JSON Schema):
    /// nombre obligatorio (≤200), tipo de área presente (por nombre o id) y profundidad acotada.
    /// </summary>
    private static void ValidateImportNodes(
        List<ImportStructureNodeDto> nodes, int depth, string path, List<string> errors)
    {
        if (depth > MaxImportDepth)
        {
            errors.Add($"{path}: se excedió la profundidad máxima permitida ({MaxImportDepth} niveles).");
            return;
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var nodePath = $"{path}[{i}]";
            if (node is null)
            {
                errors.Add($"{nodePath}: nodo inválido.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(node.Name))
            {
                errors.Add($"{nodePath}: 'name' es obligatorio.");
            }
            else if (node.Name.Trim().Length > 200)
            {
                errors.Add($"{nodePath}: 'name' supera los 200 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(node.AreaType) && !node.AreaTypeId.HasValue)
            {
                errors.Add($"{nodePath}: se requiere 'areaType' (o 'areaTypeId').");
            }

            if (node.Children is { Count: > 0 })
            {
                ValidateImportNodes(node.Children, depth + 1, $"{nodePath}.children", errors);
            }
        }
    }

    public async Task<IHttpResult<OrganizationalStructureDto>> UpdateAsync(
        int organizationId,
        Guid organizationalStructureId,
        Guid requestedById,
        UpdateOrganizationalStructureDto updateOrganizationalStructureDto
    )
    {
        if (!await HasOpenDraftAsync(organizationId))
        {
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", NoDraftError } };
            return HttpResult<OrganizationalStructureDto>.Failure("No se pudo modificar la estructura organizacional", detail, HttpStatusCode.Conflict);
        }

        //var organizationResult = await _legalEntityService.GetOrganizationByIdAsync(organizationId);
        //if (!organizationResult.IsSuccess)
        //{
        //    var title = "Error al actualizar la estructura organizacional";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo recuperar la organización"] } };
        //    return HttpResult<OrganizationalStructureDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}

        var existingOrgStructureResult = await _organizationalStructureRepository.GetAsync(o =>
            o.OrganizationId == organizationId && o.Id == organizationalStructureId
        );

        var details = new Dictionary<string, IEnumerable<string>>();
        if (!existingOrgStructureResult.IsSuccess)
        {
            var title = "Error al actualizar la estructura organizacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo recuperar la estructura organizacional"] } };
            return HttpResult<OrganizationalStructureDto>.Failure(
                title, detail, HttpStatusCode.BadRequest
            );
        }

        var existingOrgStructure = existingOrgStructureResult.Value;
        if (existingOrgStructure == null)
        {
            details.TryAdd("organizationalStructureId", ["La estructura organizacional no existe"]);

        }

        // Validación: prevenir que una estructura sea asignada como dependiente de sí misma
        if (updateOrganizationalStructureDto.ParentId.HasValue &&
            updateOrganizationalStructureDto.ParentId == organizationalStructureId)
        {
            details.TryAdd("parentId", ["Una estructura organizacional no puede ser dependiente de sí misma"]);
        }

        // Validación: prevenir ciclos en la jerarquía
        if (updateOrganizationalStructureDto.ParentId.HasValue)
        {
            var isCyclic = await IsCyclicHierarchyAsync(
                organizationId,
                organizationalStructureId,
                updateOrganizationalStructureDto.ParentId.Value
            );
            if (isCyclic)
            {
                details.TryAdd("parentId", ["No se puede asignar una estructura como dependiente de una de sus descendientes"]);
            }
        }
        var existingStructureNameResult = await _organizationalStructureRepository.GetAsync(os =>
            os.OrganizationId == organizationId
            && os.Name.ToLower().Trim() == updateOrganizationalStructureDto.Name.ToLower().Trim() && os.Id != organizationalStructureId
        );
        if (!existingStructureNameResult.IsSuccess)
        {
            var title = "Error al actualizar la estructura organizacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar la existencia del nombre de la estructura organizacional"] } };
            return HttpResult<OrganizationalStructureDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        var existingStructureName = existingStructureNameResult.Value;
        if (existingStructureName != null)
        {
            details.TryAdd("name", ["El nombre ya esta registrado"]);
        }

        if (details.Count > 0)
        {
            var title = "No se pudo actualizar la estructura organizacional";
            return HttpResult<OrganizationalStructureDto>.Failure(title, details, HttpStatusCode.BadRequest);
        }

        existingOrgStructure.AreaTypeId = updateOrganizationalStructureDto.AreaTypeId;
        existingOrgStructure!.ParentId = updateOrganizationalStructureDto.ParentId;
        existingOrgStructure.Name = updateOrganizationalStructureDto.Name;

        var updateResult = await _organizationalStructureRepository.UpdateAsync(
            existingOrgStructure
        );

        if (!updateResult.IsSuccess)
        {
            _logger.LogInformation(
                $"Failed to update organizational structure with id '{organizationalStructureId}'"
            );
            var title = "Error al actualizar la estructura organizacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo actualizar la estructura organizacional"] } };
            return HttpResult<OrganizationalStructureDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        var organizationalStructureDto = _mapper.Map<OrganizationalStructureDto>(
            existingOrgStructure
        );
        return HttpResult<OrganizationalStructureDto>.Success(organizationalStructureDto);
    }

    public async Task<IHttpResult> DeleteAsync(
        int organizationId,
        Guid organizationalStructureId,
        Guid requestedById
    )
    {
        if (!await HasOpenDraftAsync(organizationId))
        {
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", NoDraftError } };
            return HttpResult.Failure("No se pudo modificar la estructura organizacional", detail, HttpStatusCode.Conflict);
        }

        // Cargar todas las unidades vivas de la organización una sola vez y recorrer el
        // subárbol en memoria (sin N+1). FilterAsync excluye las eliminadas lógicamente.
        var allResult = await _organizationalStructureRepository.FilterAsync(o => o.OrganizationId == organizationId);
        if (!allResult.IsSuccess)
        {
            var title = "Error al eliminar la estructura organizacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo recuperar la estructura organizacional"] } };
            return HttpResult.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var all = allResult.Value.ToList();
        if (all.All(o => o.Id != organizationalStructureId))
        {
            var title = "Error al eliminar la estructura organizacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["La estructura organizacional no existe"] } };
            return HttpResult.Failure(title, detail, HttpStatusCode.NotFound);
        }

        var childrenByParent = all
            .Where(o => o.ParentId.HasValue)
            .GroupBy(o => o.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Id).ToList());

        // Recolectar el nodo objetivo y TODOS sus descendientes (no solo los hijos directos),
        // para evitar dejar nodos huérfanos vivos que luego colisionen por nombre al re-importar.
        var subtreeIds = new List<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(organizationalStructureId);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            subtreeIds.Add(id);
            if (childrenByParent.TryGetValue(id, out var kids))
                foreach (var childId in kids) stack.Push(childId);
        }

        // Bloquear la eliminación si alguna unidad del subárbol todavía tiene cargos asociados.
        var positionsResult = await _positionRepository.FilterAsync(p => subtreeIds.Contains(p.OrganizationalStructureId));
        if (!positionsResult.IsSuccess)
        {
            var title = "Error al verificar posiciones";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar las posiciones asociadas."] } };
            return HttpResult.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        if (positionsResult.Value?.Any() == true)
        {
            var title = "No se puede eliminar la estructura organizacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se puede eliminar la estructura organizacional porque tiene cargos asociados."] } };
            return HttpResult.Failure(title, detail, HttpStatusCode.Conflict);
        }

        var deleteResult = await _organizationalStructureRepository.DeleteRangeAsync(o =>
            o.OrganizationId == organizationId && subtreeIds.Contains(o.Id)
        );

        if (!deleteResult.IsSuccess)
        {
            var error = "Not found";
            _logger.LogInformation(
                $"Failed to delete organizational structure with id '{organizationalStructureId}': {error}",
                organizationalStructureId,
                error
            );
            var title = "Error al eliminar la estructura organizacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo eliminar la estructura organizacional"] } };
            return HttpResult.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        return HttpResult.Success();
    }

    public async Task<IHttpResult<OrganizationalStructureDto>> GetAsync(
        int organizationId,
        Guid organizationalStructureId,
        Guid requestedById
    )
    {
        //var organizationResult = await _legalEntityService.GetOrganizationByIdAsync(organizationId);
        //if (!organizationResult.IsSuccess)
        //{
        //    var title = "Error al obtener la estructura organizacional";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo recuperar la organización"] } };
        //    return HttpResult<OrganizationalStructureDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}

        var result = await _organizationalStructureRepository.GetAsync(
            os => os.Id == organizationalStructureId,
            query =>
                query
                    .Include(os => os.AreaType)
                    .Include(os => os.Positions.Where(p => p.DeletedAt == null))
                        .ThenInclude(p => p.OccupationalGroup)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                            .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                            .ThenInclude(d2 => d2.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                            .ThenInclude(d2 => d2.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                .ThenInclude(d3 => d3.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                    .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                            .ThenInclude(d2 => d2.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                .ThenInclude(d3 => d3.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                    .ThenInclude(d4 => d4.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                        .ThenInclude(d => d.AreaType)
        );
        if (!result.IsSuccess)
        {
            var title = "Error al obtener la estructura organizacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo recuperar la estructura organizacional"] } };
            return HttpResult<OrganizationalStructureDto>.Failure(
                title, detail, HttpStatusCode.BadRequest
            );
        }

        var organizationalStructure = result.Value;
        var organizationalStructureDto = _mapper.Map<OrganizationalStructureDto>(
            organizationalStructure
        );

        return HttpResult<OrganizationalStructureDto>.Success(organizationalStructureDto);
    }

    public async Task<IHttpResult<IEnumerable<OrganizationalStructureDto>>> ListAsync(
        int organizationId
    )
    {

        var result = await _organizationalStructureRepository.FilterAsync(
            os => os.OrganizationId == organizationId && os.ParentId == null,
            query =>
                query
                    .Include(os => os.AreaType)
                    .Include(os => os.Positions.Where(p => p.DeletedAt == null))
                        .ThenInclude(p => p.OccupationalGroup)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                            .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                            .ThenInclude(d2 => d2.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                            .ThenInclude(d2 => d2.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                .ThenInclude(d3 => d3.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                    .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                            .ThenInclude(d2 => d2.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                .ThenInclude(d3 => d3.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                    .ThenInclude(d4 => d4.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                        .ThenInclude(d => d.AreaType)
                    .OrderBy(os => os.Code)

        );

        if (!result.IsSuccess)
        {
            var title = "Error al listar las estructuras organizacionales";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudieron listar las estructuras organizacionales"] } };
            return HttpResult<IEnumerable<OrganizationalStructureDto>>.Failure(
                title, detail, HttpStatusCode.BadRequest
            );
        }

        var organizationalStructures = result.Value.ToList();
        var allPositionsResult = await _positionRepository.FilterAsync(p => p.OrganizationalStructure.OrganizationId == organizationId);
        var allPositions = allPositionsResult.IsSuccess ? allPositionsResult.Value.ToList() : new List<Position>();

        //var allPersonnelPositionsResult = await _personnelPositionRepository.GetAllAsync();
        //var allPersonnelPositions = allPersonnelPositionsResult.IsSuccess
        //? allPersonnelPositionsResult.Value.Where(pp => pp.DischargeDate == null).ToList()
        //: new List<PersonnelPosition>();

        var organizationalStructuresDto = _mapper.Map<List<OrganizationalStructureDto>>(organizationalStructures);

        void SetPersonnelCount(OrganizationalStructureDto dto)
        {
            var positionIds = allPositions
                .Where(p => p.OrganizationalStructureId == dto.Id)
                .Select(p => p.Id)
                .ToList();

            //dto.personnelCount = allPersonnelPositions.Count(pp => positionIds.Contains(pp.PositionId));

            if (dto.Dependencies != null)
            {
                foreach (var dep in dto.Dependencies)
                    SetPersonnelCount(dep);
            }
        }

        foreach (var dto in organizationalStructuresDto)
            SetPersonnelCount(dto);

        return HttpResult<IEnumerable<OrganizationalStructureDto>>.Success(organizationalStructuresDto);
    }

    public async Task<IHttpResult<PagedResponseDto<OrganizationalStructureWhioutDependenciesDto>>> ListPaginatedAsync(
        int organizationId,
        PaginationFilterDto paginationFilter,
        HttpContext httpContext,
        string? searchTerm = null,
        bool includeExternal = true
    )
    {
        var pageNumber = paginationFilter.PageNumber;
        var pageSize = paginationFilter.PageSize;
        var hasSearch = !string.IsNullOrWhiteSpace(searchTerm);

        // Determinar si la estructura virtual "Externo" aplica a esta página/búsqueda.
        var externalDto = includeExternal ? BuildExternalStructureDto() : null;
        var externalMatches = externalDto != null &&
            (!hasSearch || externalDto.Name.Contains(searchTerm!, StringComparison.OrdinalIgnoreCase));

        // Ajustar skip/take de la BD para que "Externo" ocupe siempre la posición 0.
        int dbSkip, dbTake;
        if (externalMatches)
        {
            dbSkip = pageNumber == 1 ? 0 : (pageNumber - 1) * pageSize - 1;
            dbTake = pageNumber == 1 ? pageSize - 1 : pageSize;
        }
        else
        {
            dbSkip = (pageNumber - 1) * pageSize;
            dbTake = pageSize;
        }

        var totalRecordsResult = await _organizationalStructureRepository.CountAsync(
            os => os.OrganizationId == organizationId &&
                  (!hasSearch || os.Name.Contains(searchTerm!))
        );

        var totalRecords = totalRecordsResult.Value + (externalMatches ? 1 : 0);

        var result = await _organizationalStructureRepository.FilterAsync(
            os => os.OrganizationId == organizationId &&
                  (!hasSearch || os.Name.Contains(searchTerm!)),
            query =>
                query
                    .Include(os => os.AreaType)
                    .Include(os => os.Parent)
                    .OrderBy(os => os.Code)
                    .Skip(dbSkip)
                    .Take(dbTake)
        );

        if (!result.IsSuccess)
        {
            var title = "Error al listar las estructuras organizacionales";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudieron listar las estructuras organizacionales"] } };
            return HttpResult<PagedResponseDto<OrganizationalStructureWhioutDependenciesDto>>.Failure(
                title, detail, HttpStatusCode.BadRequest
            );
        }

        var organizationalStructuresDto = _mapper.Map<List<OrganizationalStructureWhioutDependenciesDto>>(result.Value);

        if (externalMatches && pageNumber == 1)
            organizationalStructuresDto.Insert(0, externalDto!);

        var pagedResponse = _paginationService.BuildPaginationUrl(httpContext, pageNumber, pageSize, totalRecords, organizationalStructuresDto);

        return HttpResult<PagedResponseDto<OrganizationalStructureWhioutDependenciesDto>>.Success(pagedResponse);
    }

    private OrganizationalStructureWhioutDependenciesDto? BuildExternalStructureDto()
    {
        var section = _configuration.GetSection("ExternalOrganizationalStructure");
        if (!section.Exists()) return null;

        var id = section.GetValue<Guid>("Id");
        var name = section.GetValue<string>("Name") ?? "Externo";
        var code = section.GetValue<string>("Code") ?? "00";
        var areaTypeIdRaw = section.GetValue<string>("AreaTypeId");
        var areaTypeName = section.GetValue<string>("AreaTypeName");
        var areaTypeOrder = section.GetValue<int>("AreaTypeOrder");

        Guid? areaTypeId = Guid.TryParse(areaTypeIdRaw, out var parsedId) && parsedId != Guid.Empty
            ? parsedId
            : null;

        OrganizationalStructureAreaTypeDto? areaType = areaTypeId.HasValue && !string.IsNullOrWhiteSpace(areaTypeName)
            ? new OrganizationalStructureAreaTypeDto { Id = areaTypeId.Value, Name = areaTypeName, Order = areaTypeOrder }
            : null;

        return new OrganizationalStructureWhioutDependenciesDto
        {
            Id = id,
            Code = code,
            Name = name,
            AreaTypeId = areaTypeId,
            AreaType = areaType,
            Parent = null
        };
    }

    public async Task<IHttpResult<IEnumerable<OrganizationalStructureDto>>> SearchAsync(
        int organizationId,
        string searchTerm
    )
    {
        //var organizationResult = await _legalEntityService.GetOrganizationByIdAsync(organizationId);
        //if (!organizationResult.IsSuccess)
        //{
        //    var title = "Error al buscar las estructuras organizacionales";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo recuperar la organización"] } };
        //    return HttpResult<IEnumerable<OrganizationalStructureDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}

        var result = await _organizationalStructureRepository.FilterAsync(
            os => os.OrganizationId == organizationId && os.Name.Contains(searchTerm),
            query =>
                query
                    .Include(os => os.AreaType)
                    .Include(os => os.Positions.Where(p => p.DeletedAt == null))
                        .ThenInclude(p => p.OccupationalGroup)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                            .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                            .ThenInclude(d2 => d2.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                            .ThenInclude(d2 => d2.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                .ThenInclude(d3 => d3.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                    .ThenInclude(d => d.AreaType)
                    .Include(os => os.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                        .ThenInclude(d1 => d1.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                            .ThenInclude(d2 => d2.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                .ThenInclude(d3 => d3.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                    .ThenInclude(d4 => d4.Dependencies.Where(d => d.DeletedAt == null).OrderBy(d => d.Code))
                                        .ThenInclude(d => d.AreaType)
                    .OrderBy(os => os.Code)
        );
        if (!result.IsSuccess)
        {
            var title = "Error al buscar las estructuras organizacionales";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudieron buscar las estructuras organizacionales"] } };
            return HttpResult<IEnumerable<OrganizationalStructureDto>>.Failure(
                title, detail, HttpStatusCode.BadRequest
            );
        }
        var organizationalStructures = result.Value.ToList();
        var allPositionsResult = await _positionRepository.FilterAsync(p => p.OrganizationalStructure.OrganizationId == organizationId);
        var allPositions = allPositionsResult.IsSuccess ? allPositionsResult.Value.ToList() : new List<Position>();

        //var allPersonnelPositionsResult = await _personnelPositionRepository.GetAllAsync();
        //var allPersonnelPositions = allPersonnelPositionsResult.IsSuccess
        //? allPersonnelPositionsResult.Value.Where(pp => pp.DischargeDate == null).ToList()
        //: new List<PersonnelPosition>();

        var organizationalStructuresDto = _mapper.Map<List<OrganizationalStructureDto>>(organizationalStructures);

        void SetPersonnelCount(OrganizationalStructureDto dto)
        {
            var positionIds = allPositions
                .Where(p => p.OrganizationalStructureId == dto.Id)
                .Select(p => p.Id)
                .ToList();

            //dto.personnelCount = allPersonnelPositions.Count(pp => positionIds.Contains(pp.PositionId));

            if (dto.Dependencies != null)
            {
                foreach (var dep in dto.Dependencies)
                    SetPersonnelCount(dep);
            }
        }

        foreach (var dto in organizationalStructuresDto)
            SetPersonnelCount(dto);
        return HttpResult<IEnumerable<OrganizationalStructureDto>>.Success(organizationalStructuresDto);
    }

    /// <summary>
    /// Verifica si la rama de ancestros ya contiene una relación cíclica.
    /// </summary>
    private async Task<bool> HasCycleInAncestryAsync(int organizationId, Guid startStructureId)
    {
        Guid? currentParentId = startStructureId;
        var visitedIds = new HashSet<Guid>();

        while (currentParentId.HasValue && currentParentId.Value != Guid.Empty)
        {
            if (!visitedIds.Add(currentParentId.Value))
            {
                return true;
            }

            var parentResult = await _organizationalStructureRepository.GetAsync(os =>
                os.OrganizationId == organizationId && os.Id == currentParentId.Value
            );

            if (!parentResult.IsSuccess || parentResult.Value is null)
            {
                return false;
            }

            currentParentId = parentResult.Value.ParentId;
        }

        return false;
    }

    /// <summary>
    /// Valida si asignar un parentId crearría un ciclo en la jerarquía.
    /// Retorna true si se detecta un ciclo (relación cíclica).
    /// </summary>
    private async Task<bool> IsCyclicHierarchyAsync(
        int organizationId,
        Guid currentStructureId,
        Guid proposedParentId
    )
    {
        // Si el padre propuesto es la misma estructura, es un ciclo
        if (currentStructureId == proposedParentId)
        {
            return true;
        }

        // Rastrear todos los ancestros del padre propuesto
        Guid? currentParentId = proposedParentId; // Cambiar a Guid? e inicializar correctamente
        var visitedIds = new HashSet<Guid> { proposedParentId };

        while (currentParentId.HasValue && currentParentId != Guid.Empty)
        {
            // Si encontramos que la estructura actual es un ancestro del padre propuesto, hay ciclo
            if (currentParentId == currentStructureId)
            {
                return true;
            }

            // Obtener el padre del padre propuesto
            var parentResult = await _organizationalStructureRepository.GetAsync(os =>
                os.OrganizationId == organizationId && os.Id == currentParentId
            );

            if (!parentResult.IsSuccess || parentResult.Value == null)
            {
                break;
            }

            currentParentId = parentResult.Value.ParentId;

            // Detectar ciclo infinito en la búsqueda (por seguridad)
            if (currentParentId.HasValue && !visitedIds.Add(currentParentId.Value))
            {
                return true;
            }
        }

        return false;
    }
    public async Task<IResult<bool>> AnyAsync(int organizationId, Guid organizationalStructureId)
    {
        var result = await _organizationalStructureRepository.AnyAsync(os =>
            os.OrganizationId == organizationId && os.Id == organizationalStructureId
        );

        return result;
    }
}
