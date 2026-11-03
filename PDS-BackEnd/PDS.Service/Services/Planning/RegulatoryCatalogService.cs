using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PDS.Application.Data;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;
using PDS.Service.Services.Excel;

namespace PDS.Service.Services.Planning;

public class RegulatoryCatalogService : IRegulatoryCatalogService
{
    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;

    public RegulatoryCatalogService(IUnitOfWork<PDSDbContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private static readonly Dictionary<string, string> StandardExpenseObjectNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1"] = "GASTOS GENERALES",
        ["2"] = "GASTOS CORRIENTES Y DE CONSUMO",
        ["2.1"] = "REMUNERACIONES Y CONTRIBUCIONES",
        ["2.1.1"] = "REMUNERACIONES AL PERSONAL FIJO",
        ["2.1.2"] = "REMUNERACIONES AL PERSONAL TEMPORAL",
        ["2.1.3"] = "DIETAS Y GASTOS DE REPRESENTACIÓN",
        ["2.1.4"] = "GRATIFICACIONES Y BONIFICACIONES",
        ["2.1.5"] = "CONTRIBUCIONES A LA SEGURIDAD SOCIAL",
        ["2.2"] = "CONTRATACIÓN DE SERVICIOS",
        ["2.2.1"] = "SERVICIOS BÁSICOS",
        ["2.2.2"] = "PUBLICIDAD, IMPRESIÓN Y ENCUADERNACIÓN",
        ["2.2.3"] = "VIÁTICOS Y PASAJES",
        ["2.2.4"] = "SERVICIOS DE TRANSPORTE Y ALMACENAJE",
        ["2.2.5"] = "ALQUILERES Y RENTAS",
        ["2.2.6"] = "SEGUROS Y FIANZAS",
        ["2.2.7"] = "SERVICIOS DE CONSERVACIÓN, REPARACIONES Y MANTENIMIENTO",
        ["2.2.8"] = "SERVICIOS TÉCNICOS Y PROFESIONALES",
        ["2.3"] = "MATERIALES Y SUMINISTROS",
        ["2.3.1"] = "ALIMENTOS Y PRODUCTOS AGROFORESTALES",
        ["2.3.2"] = "TEXTILES Y VESTUARIOS",
        ["2.3.3"] = "PRODUCTOS DE PAPEL, CARTÓN E IMPRESOS",
        ["2.3.4"] = "PRODUCTOS QUÍMICOS Y CONEXOS",
        ["2.3.5"] = "PRODUCTOS DE CUERO, CAUCHO Y PLÁSTICO",
        ["2.3.6"] = "PRODUCTOS DE MINERALES NO METÁLICOS Y METÁLICOS",
        ["2.3.7"] = "COMBUSTIBLES, LUBRICANTES, PRODUCTOS QUÍMICOS Y CONEXOS",
        ["2.3.8"] = "HERRAMIENTAS, REPUESTOS Y ACCESORIOS",
        ["2.3.9"] = "PRODUCTOS Y ÚTILES VARIOS",
        ["2.4"] = "TRANSFERENCIAS CORRIENTES",
        ["2.5"] = "TRANSFERENCIAS DE CAPITAL",
        ["2.6"] = "BIENES MUEBLES, INMUEBLES E INTANGIBLES",
        ["2.6.1"] = "MOBILIARIO Y EQUIPO",
        ["2.6.2"] = "EQUIPO DE CÓMPUTO Y COMUNICACIONES",
        ["2.6.3"] = "EQUIPO DE TRANSPORTE, TRACCIÓN Y ELEVACIÓN",
        ["2.6.4"] = "MAQUINARIA, OTROS EQUIPOS Y HERRAMIENTAS",
        ["2.7"] = "OBRAS",
        ["2.7.1"] = "OBRAS EN EDIFICACIONES",
        ["2.7.2"] = "OBRAS DE INFRAESTRUCTURA",
        ["2.8"] = "ADQUISICIÓN DE VALORES",
        ["2.9"] = "GASTOS DE LA DEUDA PÚBLICA"
    };

    public async Task<IHttpResult<PagedResponseDto<ExpenseObjectDto>>> ListExpenseObjectsAsync(int organizationId, string? q = null, bool? onlyDetail = null, int page = 1, int pageSize = 50)
    {
        await EnsureExpenseObjectHierarchyAsync(organizationId);

        var query = _unitOfWork.Context.ExpenseObjects
            .AsNoTracking()
            .Where(e => e.OrganizationId == organizationId && e.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var norm = ExcelTableReader.Normalize(q);
            query = query.Where(e => e.Code.Contains(q) || EF.Functions.Like(e.Name, $"%{q}%"));
        }

        if (onlyDetail == true)
        {
            query = query.Where(e => e.IsDetail);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExpenseObjectDto
            {
                Id = e.Id,
                Code = e.Code,
                Name = e.Name,
                Description = e.Description,
                Level = e.Level,
                ParentCode = e.ParentCode,
                ParentId = e.ParentId,
                IsDetail = e.IsDetail
            })
            .ToListAsync();

        return HttpResult<PagedResponseDto<ExpenseObjectDto>>.Success(new PagedResponseDto<ExpenseObjectDto>(items, page, pageSize, total));
    }

    private async Task EnsureExpenseObjectHierarchyAsync(int organizationId)
    {
        var existing = await _unitOfWork.Context.ExpenseObjects
            .Where(e => e.OrganizationId == organizationId && e.DeletedAt == null)
            .ToListAsync();

        if (existing.Count == 0) return;

        var existingMap = existing.ToDictionary(e => e.Code.Trim(), StringComparer.OrdinalIgnoreCase);
        bool needsSave = false;

        // 1. Corregir / asignar Level si está en 0
        foreach (var item in existing)
        {
            var segments = item.Code.Split('.').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            var calcLevel = segments.Length == 0 ? 4 : Math.Min(segments.Length, 4);
            if (item.Level != calcLevel)
            {
                item.Level = calcLevel;
                item.IsDetail = calcLevel >= 4;
                item.UpdatedAt = DateTime.UtcNow;
                needsSave = true;
            }
        }

        // 2. Generar padres faltantes (Niveles 1, 2 y 3)
        var codesToInspect = existingMap.Keys.ToList();
        foreach (var code in codesToInspect)
        {
            var segments = code.Split('.').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            if (segments.Length <= 1) continue;

            for (int i = 1; i < segments.Length && i <= 3; i++)
            {
                var parentCode = string.Join(".", segments.Take(i));
                if (!existingMap.ContainsKey(parentCode))
                {
                    var parentName = StandardExpenseObjectNames.GetValueOrDefault(parentCode, $"Objeto del Gasto {parentCode}");
                    var newParent = new ExpenseObject
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = organizationId,
                        Code = parentCode,
                        Name = parentName,
                        Description = $"Categoría acumuladora de nivel {i} ({parentName})",
                        Level = i,
                        IsDetail = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _unitOfWork.Context.ExpenseObjects.Add(newParent);
                    existingMap[parentCode] = newParent;
                    needsSave = true;
                }
            }
        }

        if (needsSave)
        {
            await _unitOfWork.Context.SaveChangesAsync();
        }
    }


    public async Task<IHttpResult<PagedResponseDto<AccountingAccountDto>>> ListAccountingAccountsAsync(int organizationId, string? q = null, bool? onlyPostable = null, int page = 1, int pageSize = 50)
    {
        await EnsureMastersoftCatalogSeededAsync(organizationId);

        var query = _unitOfWork.Context.AccountingAccounts
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && a.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var norm = ExcelTableReader.Normalize(q);
            query = query.Where(a => a.Code.Contains(q) || EF.Functions.Like(a.Name, $"%{q}%"));
        }

        if (onlyPostable == true)
        {
            query = query.Where(a => a.IsPostable);
        }

        var total = await query.CountAsync();
        var rawItems = await query
            .OrderBy(a => a.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.Code,
                a.Name,
                a.IsPostable,
                a.Level,
                a.ParentCode,
                a.ParentId,
                a.ExtendedDescription
            })
            .ToListAsync();

        var directMappings = new Dictionary<string, (string MasterCode, string MasterName)>(StringComparer.OrdinalIgnoreCase);
        var digecobParents = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var rawMappings = await _unitOfWork.Context.MastersoftDigecobMappings
                .AsNoTracking()
                .Where(m => m.OrganizationId == organizationId)
                .Include(m => m.MastersoftAccount)
                .ToListAsync();

            foreach (var m in rawMappings)
            {
                var key = m.DigecobAccountCode?.Trim();
                if (!string.IsNullOrEmpty(key) && !directMappings.ContainsKey(key))
                {
                    directMappings[key] = (m.MastersoftAccountCode, m.MastersoftAccount?.Name ?? string.Empty);
                }
            }

            var rawParents = await _unitOfWork.Context.AccountingAccounts
                .AsNoTracking()
                .Where(a => a.OrganizationId == organizationId && a.DeletedAt == null)
                .Select(a => new { a.Code, a.ParentCode })
                .ToListAsync();

            foreach (var a in rawParents)
            {
                var key = a.Code?.Trim();
                if (!string.IsNullOrEmpty(key) && !digecobParents.ContainsKey(key))
                {
                    digecobParents[key] = a.ParentCode;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not load Mastersoft mappings for accounting accounts: {ex.Message}");
        }

        var items = rawItems.Select(a =>
        {
            var res = ResolveMastersoftMapping(a.Code, directMappings, digecobParents);
            return new AccountingAccountDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                IsPostable = a.IsPostable,
                Level = a.Level,
                ParentCode = a.ParentCode,
                ParentId = a.ParentId,
                ExtendedDescription = a.ExtendedDescription,
                MastersoftAccountCode = res.MastersoftCode,
                MastersoftAccountName = res.MastersoftName,
                IsMastersoftInherited = res.IsInherited,
                InheritedFromDigecobCode = res.InheritedFromDigecobCode
            };
        }).ToList();

        return HttpResult<PagedResponseDto<AccountingAccountDto>>.Success(new PagedResponseDto<AccountingAccountDto>(items, page, pageSize, total));
    }

    public async Task<IHttpResult<IReadOnlyList<ConsolidatorAreaProfileDto>>> ListConsolidatorAreaProfilesAsync(int organizationId)
    {
        var profiles = await _unitOfWork.Context.ConsolidatorAreaProfiles
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.DeletedAt == null)
            .OrderBy(p => p.AreaCode)
            .Select(p => new ConsolidatorAreaProfileDto
            {
                Id = p.Id,
                AreaCode = p.AreaCode,
                OrganicGroup = p.OrganicGroup,
                AreaName = p.AreaName,
                OrganizationalStructureId = p.OrganizationalStructureId,
                MandateSummary = p.MandateSummary,
                ConsolidatorKind = p.ConsolidatorKind
            })
            .ToListAsync();

        return HttpResult<IReadOnlyList<ConsolidatorAreaProfileDto>>.Success(profiles);
    }

    // ───────────── Catálogo Regulatorio MasterSoft ─────────────

    public async Task<IHttpResult<PagedResponseDto<MastersoftAccountDto>>> ListMastersoftAccountsAsync(int organizationId, string? q = null, int page = 1, int pageSize = 50)
    {
        await EnsureMastersoftCatalogSeededAsync(organizationId);

        try
        {
            var query = _unitOfWork.Context.MastersoftAccounts
                .AsNoTracking()
                .Where(m => m.OrganizationId == organizationId && m.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(m => m.Code.Contains(q) || EF.Functions.Like(m.Name, $"%{q}%"));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(m => m.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MastersoftAccountDto
                {
                    Id = m.Id,
                    Code = m.Code,
                    Name = m.Name,
                    ControlAccountCode = m.ControlAccountCode,
                    ControlAccountId = m.ControlAccountId,
                    Level = m.Level,
                    IsPostable = m.IsPostable
                })
                .ToListAsync();

            return HttpResult<PagedResponseDto<MastersoftAccountDto>>.Success(new PagedResponseDto<MastersoftAccountDto>(items, page, pageSize, total));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error querying MastersoftAccounts: {ex.Message}");
            return HttpResult<PagedResponseDto<MastersoftAccountDto>>.Success(new PagedResponseDto<MastersoftAccountDto>(new List<MastersoftAccountDto>(), page, pageSize, 0));
        }
    }

    public async Task<IHttpResult<PagedResponseDto<MastersoftDigecobMappingDto>>> ListMastersoftMappingsAsync(int organizationId, string? q = null, string? filterMode = null, int page = 1, int pageSize = 50)
    {
        await EnsureMastersoftCatalogSeededAsync(organizationId);

        var digecobAccounts = await _unitOfWork.Context.AccountingAccounts
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && a.DeletedAt == null)
            .OrderBy(a => a.Code)
            .ToListAsync();

        var directMappings = new Dictionary<string, MastersoftDigecobMapping>(StringComparer.OrdinalIgnoreCase);
        var directMapTuples = new Dictionary<string, (string MasterCode, string MasterName)>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var rawMappings = await _unitOfWork.Context.MastersoftDigecobMappings
                .AsNoTracking()
                .Where(m => m.OrganizationId == organizationId)
                .Include(m => m.MastersoftAccount)
                .ToListAsync();

            foreach (var m in rawMappings)
            {
                var key = m.DigecobAccountCode?.Trim();
                if (!string.IsNullOrEmpty(key) && !directMappings.ContainsKey(key))
                {
                    directMappings[key] = m;
                    directMapTuples[key] = (m.MastersoftAccountCode, m.MastersoftAccount?.Name ?? string.Empty);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not load direct mappings: {ex.Message}");
        }

        var digecobParents = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in digecobAccounts)
        {
            var key = a.Code?.Trim();
            if (!string.IsNullOrEmpty(key) && !digecobParents.ContainsKey(key))
            {
                digecobParents[key] = a.ParentCode;
            }
        }

        var allDtos = new List<MastersoftDigecobMappingDto>();
        foreach (var dig in digecobAccounts)
        {
            directMappings.TryGetValue(dig.Code.Trim(), out var direct);
            var res = ResolveMastersoftMapping(dig.Code, directMapTuples, digecobParents);

            allDtos.Add(new MastersoftDigecobMappingDto
            {
                Id = direct?.Id ?? Guid.Empty,
                DigecobAccountId = dig.Id,
                DigecobAccountCode = dig.Code,
                DigecobAccountName = dig.Name,
                DigecobIsPostable = dig.IsPostable,
                DigecobLevel = dig.Level,
                MastersoftAccountId = direct?.MastersoftAccountId,
                MastersoftAccountCode = res.MastersoftCode,
                MastersoftAccountName = res.MastersoftName,
                IsInherited = res.IsInherited,
                InheritedFromDigecobCode = res.InheritedFromDigecobCode
            });
        }

        IEnumerable<MastersoftDigecobMappingDto> filtered = allDtos;

        if (!string.IsNullOrWhiteSpace(q))
        {
            filtered = filtered.Where(d =>
                d.DigecobAccountCode.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                d.DigecobAccountName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (d.MastersoftAccountCode != null && d.MastersoftAccountCode.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (d.MastersoftAccountName != null && d.MastersoftAccountName.Contains(q, StringComparison.OrdinalIgnoreCase))
            );
        }

        if (!string.IsNullOrWhiteSpace(filterMode))
        {
            switch (filterMode.ToLowerInvariant())
            {
                case "linked":
                    filtered = filtered.Where(d => d.MastersoftAccountCode != null);
                    break;
                case "unlinked":
                    filtered = filtered.Where(d => d.MastersoftAccountCode == null);
                    break;
                case "direct":
                    filtered = filtered.Where(d => d.MastersoftAccountCode != null && !d.IsInherited);
                    break;
                case "inherited":
                    filtered = filtered.Where(d => d.IsInherited);
                    break;
            }
        }

        var total = filtered.Count();
        var pagedItems = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return HttpResult<PagedResponseDto<MastersoftDigecobMappingDto>>.Success(new PagedResponseDto<MastersoftDigecobMappingDto>(pagedItems, page, pageSize, total));
    }

    public async Task<IHttpResult<MastersoftDigecobMappingDto>> CreateOrUpdateMastersoftMappingAsync(int organizationId, CreateMastersoftMappingDto dto)
    {
        var digecob = await _unitOfWork.Context.AccountingAccounts
            .FirstOrDefaultAsync(a => a.Id == dto.DigecobAccountId && a.OrganizationId == organizationId && a.DeletedAt == null);
        if (digecob == null)
            return HttpResult<MastersoftDigecobMappingDto>.Failure("Cuenta DIGECOB no encontrada", new Dictionary<string, IEnumerable<string>> { { "digecobAccountId", new[] { "Cuenta DIGECOB no encontrada" } } }, System.Net.HttpStatusCode.NotFound);

        var mastersoft = await _unitOfWork.Context.MastersoftAccounts
            .FirstOrDefaultAsync(m => m.Id == dto.MastersoftAccountId && m.OrganizationId == organizationId && m.DeletedAt == null);
        if (mastersoft == null)
            return HttpResult<MastersoftDigecobMappingDto>.Failure("Cuenta MasterSoft no encontrada", new Dictionary<string, IEnumerable<string>> { { "mastersoftAccountId", new[] { "Cuenta MasterSoft no encontrada" } } }, System.Net.HttpStatusCode.NotFound);

        var existing = await _unitOfWork.Context.MastersoftDigecobMappings
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.DigecobAccountId == dto.DigecobAccountId);

        if (existing == null)
        {
            existing = new MastersoftDigecobMapping
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                DigecobAccountId = digecob.Id,
                DigecobAccountCode = digecob.Code,
                MastersoftAccountId = mastersoft.Id,
                MastersoftAccountCode = mastersoft.Code,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _unitOfWork.Context.MastersoftDigecobMappings.Add(existing);
        }
        else
        {
            existing.MastersoftAccountId = mastersoft.Id;
            existing.MastersoftAccountCode = mastersoft.Code;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.Context.SaveChangesAsync();

        var resultDto = new MastersoftDigecobMappingDto
        {
            Id = existing.Id,
            DigecobAccountId = digecob.Id,
            DigecobAccountCode = digecob.Code,
            DigecobAccountName = digecob.Name,
            DigecobIsPostable = digecob.IsPostable,
            DigecobLevel = digecob.Level,
            MastersoftAccountId = mastersoft.Id,
            MastersoftAccountCode = mastersoft.Code,
            MastersoftAccountName = mastersoft.Name,
            IsInherited = false
        };

        return HttpResult<MastersoftDigecobMappingDto>.Success(resultDto);
    }

    public async Task<IHttpResult<bool>> DeleteMastersoftMappingAsync(int organizationId, Guid mappingId)
    {
        var existing = await _unitOfWork.Context.MastersoftDigecobMappings
            .FirstOrDefaultAsync(m => m.Id == mappingId && m.OrganizationId == organizationId);

        if (existing == null)
            return HttpResult<bool>.Failure("Vinculación no encontrada", new Dictionary<string, IEnumerable<string>> { { "mappingId", new[] { "Vinculación no encontrada" } } }, System.Net.HttpStatusCode.NotFound);

        _unitOfWork.Context.MastersoftDigecobMappings.Remove(existing);
        await _unitOfWork.Context.SaveChangesAsync();
        return HttpResult<bool>.Success(true);
    }

    public async Task<IHttpResult<MastersoftImportSummaryDto>> ImportMastersoftCatalogAndMappingsAsync(int organizationId, System.IO.Stream stream)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var summary = new MastersoftImportSummaryDto();

        // 1. Sheet MasterSoft Catalog (Catálogo de Máster Soft)
        var sheetMaster = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("Master", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Soft", StringComparison.OrdinalIgnoreCase));
        if (sheetMaster == null && workbook.Worksheets.Count >= 2)
        {
            sheetMaster = workbook.Worksheet(2);
        }

        var rawMasterAccounts = await _unitOfWork.Context.MastersoftAccounts
            .Where(m => m.OrganizationId == organizationId && m.DeletedAt == null)
            .ToListAsync();

        var masterMap = new Dictionary<string, MastersoftAccount>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in rawMasterAccounts)
        {
            var key = m.Code?.Trim();
            if (!string.IsNullOrEmpty(key) && !masterMap.ContainsKey(key))
            {
                masterMap[key] = m;
            }
        }

        if (sheetMaster != null)
        {
            var rows = sheetMaster.RowsUsed().Skip(1); // Saltar encabezado
            foreach (var row in rows)
            {
                var code = row.Cell(1).GetString().Trim();
                var name = row.Cell(2).GetString().Trim();
                var ctaControl = row.Cell(3).GetString().Trim();

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name)) continue;

                if (masterMap.TryGetValue(code, out var existingMaster))
                {
                    existingMaster.Name = name;
                    existingMaster.ControlAccountCode = string.IsNullOrEmpty(ctaControl) || ctaControl == ".." ? null : ctaControl;
                    existingMaster.UpdatedAt = DateTime.UtcNow;
                    summary.MastersoftAccountsUpdated++;
                }
                else
                {
                    var newMaster = new MastersoftAccount
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = organizationId,
                        Code = code,
                        Name = name,
                        ControlAccountCode = string.IsNullOrEmpty(ctaControl) || ctaControl == ".." ? null : ctaControl,
                        Level = code.Contains('-') ? 5 : code.Length,
                        IsPostable = code.Contains('-') || code.Length >= 4,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _unitOfWork.Context.MastersoftAccounts.Add(newMaster);
                    masterMap[code] = newMaster;
                    summary.MastersoftAccountsCreated++;
                }
            }
            await _unitOfWork.Context.SaveChangesAsync();

            // Asignar ControlAccountId FK
            foreach (var master in masterMap.Values)
            {
                if (!string.IsNullOrEmpty(master.ControlAccountCode) && masterMap.TryGetValue(master.ControlAccountCode, out var parentMaster))
                {
                    master.ControlAccountId = parentMaster.Id;
                }
            }
            await _unitOfWork.Context.SaveChangesAsync();
        }

        // 2. Sheet Vinculacion
        var sheetVinculacion = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("Vinc", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Homolog", StringComparison.OrdinalIgnoreCase));
        if (sheetVinculacion == null && workbook.Worksheets.Count >= 3)
        {
            sheetVinculacion = workbook.Worksheet(3);
        }

        if (sheetVinculacion != null)
        {
            var rawDigecob = await _unitOfWork.Context.AccountingAccounts
                .Where(a => a.OrganizationId == organizationId && a.DeletedAt == null)
                .ToListAsync();

            var digecobMap = new Dictionary<string, AccountingAccount>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in rawDigecob)
            {
                var key = a.Code?.Trim();
                if (!string.IsNullOrEmpty(key) && !digecobMap.ContainsKey(key))
                    digecobMap[key] = a;
            }

            var rawExisting = await _unitOfWork.Context.MastersoftDigecobMappings
                .Where(m => m.OrganizationId == organizationId)
                .ToListAsync();

            var existingMappings = new Dictionary<string, MastersoftDigecobMapping>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in rawExisting)
            {
                var key = m.DigecobAccountCode?.Trim();
                if (!string.IsNullOrEmpty(key) && !existingMappings.ContainsKey(key))
                    existingMappings[key] = m;
            }

            var rows = sheetVinculacion.RowsUsed().Skip(1);
            foreach (var row in rows)
            {
                var digecobCode = row.Cell(1).GetString().Trim();
                var masterCode = row.Cell(3).GetString().Trim();

                if (string.IsNullOrEmpty(digecobCode)) continue;
                summary.DigecobAccountsProcessed++;

                if (string.IsNullOrEmpty(masterCode)) continue;

                if (!digecobMap.TryGetValue(digecobCode, out var digAccount))
                {
                    summary.Warnings.Add($"Cuenta DIGECOB '{digecobCode}' en Vinculación no encontrada en el catálogo principal");
                    continue;
                }

                if (!masterMap.TryGetValue(masterCode, out var masterAccount))
                {
                    summary.Warnings.Add($"Cuenta MasterSoft '{masterCode}' vinculada a DIGECOB '{digecobCode}' no encontrada en el catálogo MasterSoft");
                    continue;
                }

                if (existingMappings.TryGetValue(digecobCode, out var map))
                {
                    map.MastersoftAccountId = masterAccount.Id;
                    map.MastersoftAccountCode = masterAccount.Code;
                    map.UpdatedAt = DateTime.UtcNow;
                    summary.MappingsUpdated++;
                }
                else
                {
                    var newMap = new MastersoftDigecobMapping
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = organizationId,
                        DigecobAccountId = digAccount.Id,
                        DigecobAccountCode = digAccount.Code,
                        MastersoftAccountId = masterAccount.Id,
                        MastersoftAccountCode = masterAccount.Code,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _unitOfWork.Context.MastersoftDigecobMappings.Add(newMap);
                    existingMappings[digecobCode] = newMap;
                    summary.MappingsCreated++;
                }
            }
            await _unitOfWork.Context.SaveChangesAsync();
        }

        return HttpResult<MastersoftImportSummaryDto>.Success(summary);
    }

    public async Task<IHttpResult<byte[]>> ExportMastersoftHomologationReportAsync(int organizationId)
    {
        await EnsureMastersoftCatalogSeededAsync(organizationId);

        var digecobAccounts = await _unitOfWork.Context.AccountingAccounts
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && a.DeletedAt == null)
            .OrderBy(a => a.Code)
            .ToListAsync();

        var directMappings = new Dictionary<string, (string MasterCode, string MasterName)>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var rawMappings = await _unitOfWork.Context.MastersoftDigecobMappings
                .AsNoTracking()
                .Where(m => m.OrganizationId == organizationId)
                .Include(m => m.MastersoftAccount)
                .ToListAsync();

            foreach (var m in rawMappings)
            {
                var key = m.DigecobAccountCode?.Trim();
                if (!string.IsNullOrEmpty(key) && !directMappings.ContainsKey(key))
                {
                    directMappings[key] = (m.MastersoftAccountCode, m.MastersoftAccount?.Name ?? string.Empty);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not load mappings for export: {ex.Message}");
        }

        var digecobParents = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in digecobAccounts)
        {
            var key = a.Code?.Trim();
            if (!string.IsNullOrEmpty(key) && !digecobParents.ContainsKey(key))
            {
                digecobParents[key] = a.ParentCode;
            }
        }

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("Reporte Homologación");

        ws.Cell(1, 1).Value = "Código DIGECOB";
        ws.Cell(1, 2).Value = "Descripción DIGECOB";
        ws.Cell(1, 3).Value = "Nivel DIGECOB";
        ws.Cell(1, 4).Value = "Imputable";
        ws.Cell(1, 5).Value = "Estado Vinculación";
        ws.Cell(1, 6).Value = "Código MasterSoft";
        ws.Cell(1, 7).Value = "Descripción MasterSoft";
        ws.Cell(1, 8).Value = "Heredado de DIGECOB";

        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1E293B");
        headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

        int r = 2;
        foreach (var dig in digecobAccounts)
        {
            var res = ResolveMastersoftMapping(dig.Code, directMappings, digecobParents);

            string linkStatus = res.MastersoftCode == null
                ? "Sin Vinculación"
                : (res.IsInherited ? "Heredada" : "Directa");

            ws.Cell(r, 1).Value = dig.Code;
            ws.Cell(r, 2).Value = dig.Name;
            ws.Cell(r, 3).Value = dig.Level;
            ws.Cell(r, 4).Value = dig.IsPostable ? "S" : "N";
            ws.Cell(r, 5).Value = linkStatus;
            ws.Cell(r, 6).Value = res.MastersoftCode ?? "";
            ws.Cell(r, 7).Value = res.MastersoftName ?? "";
            ws.Cell(r, 8).Value = res.InheritedFromDigecobCode ?? "";

            if (res.MastersoftCode == null)
            {
                ws.Cell(r, 5).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#D97706"); // Amber
            }
            else if (res.IsInherited)
            {
                ws.Cell(r, 5).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#2563EB"); // Blue
            }
            else
            {
                ws.Cell(r, 5).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#16A34A"); // Green
            }

            r++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return HttpResult<byte[]>.Success(ms.ToArray());
    }

    private async Task EnsureMastersoftCatalogSeededAsync(int organizationId)
    {
        try
        {
            var createTablesSql = @"
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Planning')
BEGIN
    EXEC('CREATE SCHEMA [Planning]');
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'MastersoftAccounts' AND schema_id = SCHEMA_ID('Planning')
   AND EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Planning.MastersoftAccounts') AND name = 'Id' AND system_type_id = 231)) -- 231 = sys.types nvarchar
BEGIN
    DROP TABLE [Planning].[MastersoftDigecobMappings];
    DROP TABLE [Planning].[MastersoftAccounts];
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MastersoftAccounts' AND schema_id = SCHEMA_ID('Planning'))
BEGIN
    CREATE TABLE [Planning].[MastersoftAccounts] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [OrganizationId] INT NOT NULL,
        [Code] NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(300) NOT NULL,
        [ControlAccountCode] NVARCHAR(100) NULL,
        [ControlAccountId] UNIQUEIDENTIFIER NULL,
        [Level] INT NOT NULL DEFAULT 4,
        [IsPostable] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [DeletedAt] DATETIME2 NULL
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MastersoftDigecobMappings' AND schema_id = SCHEMA_ID('Planning'))
BEGIN
    CREATE TABLE [Planning].[MastersoftDigecobMappings] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [OrganizationId] INT NOT NULL,
        [DigecobAccountId] UNIQUEIDENTIFIER NOT NULL,
        [DigecobAccountCode] NVARCHAR(100) NOT NULL,
        [MastersoftAccountId] UNIQUEIDENTIFIER NOT NULL,
        [MastersoftAccountCode] NVARCHAR(100) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END";

            await _unitOfWork.Context.Database.ExecuteSqlRawAsync(createTablesSql);

            var existingCount = await _unitOfWork.Context.MastersoftAccounts
                .CountAsync(m => m.OrganizationId == organizationId && m.DeletedAt == null);

            if (existingCount > 0) return;

            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "docs", "link con MasterSoft", "1- Homologacion Cuentas Contables.xlsx"),
                Path.Combine(Directory.GetCurrentDirectory(), "docs", "link con MasterSoft", "1- Homologacion Cuentas Contables.xlsx"),
            };

            string? foundPath = possiblePaths.FirstOrDefault(File.Exists);
            if (foundPath != null)
            {
                using var stream = File.OpenRead(foundPath);
                await ImportMastersoftCatalogAndMappingsAsync(organizationId, stream);
            }

            if (await _unitOfWork.Context.MastersoftAccounts.CountAsync(m => m.OrganizationId == organizationId && m.DeletedAt == null) == 0)
            {
                var defaultMasterAccounts = new List<MastersoftAccount>
                {
                    new MastersoftAccount { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = "1.1.01.01.001", Name = "BANCO RESERVAS DE LA REP. DOM. - CTA. CORRIENTE INST.", ControlAccountCode = "1.1.01.01", Level = 4, IsPostable = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new MastersoftAccount { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = "1.1.04.01.001", Name = "INVENTARIOS DE MATERIALES Y SUMINISTROS DE OFICINA", ControlAccountCode = "1.1.04.01", Level = 4, IsPostable = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new MastersoftAccount { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = "1.2.03.01.001", Name = "EQUIPOS DE COMPUTACION Y TECNOLOGIA DE LA INFORMACION", ControlAccountCode = "1.2.03.01", Level = 4, IsPostable = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new MastersoftAccount { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = "1.2.03.02.001", Name = "MOBILIARIO, MUEBLES Y EQUIPOS DE OFICINA", ControlAccountCode = "1.2.03.02", Level = 4, IsPostable = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new MastersoftAccount { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = "1.2.04.01.001", Name = "VEHICULOS DE TRANSPORTE TERRESTRE Y AUTOMOTOR", ControlAccountCode = "1.2.04.01", Level = 4, IsPostable = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new MastersoftAccount { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = "2.1.01.01.001", Name = "CUENTAS POR PAGAR A PROVEEDORES NACIONALES", ControlAccountCode = "2.1.01.01", Level = 4, IsPostable = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new MastersoftAccount { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = "5.1.01.01.001", Name = "SUELDOS Y SALARIOS FIJOS AL PERSONAL NOMBRADO", ControlAccountCode = "5.1.01.01", Level = 4, IsPostable = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new MastersoftAccount { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = "5.1.02.07.001", Name = "GASTOS POR SERVICIOS DE MANTENIMIENTO Y REPARACION", ControlAccountCode = "5.1.02.07", Level = 4, IsPostable = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new MastersoftAccount { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = "5.1.03.01.001", Name = "GASTOS DE MATERIALES Y UTILES DE OFICINA", ControlAccountCode = "5.1.03.01", Level = 4, IsPostable = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                };

                _unitOfWork.Context.MastersoftAccounts.AddRange(defaultMasterAccounts);
                await _unitOfWork.Context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not auto-seed MasterSoft catalog: {ex.Message}");
        }
    }

    public static MastersoftMappingResolution ResolveMastersoftMapping(
        string digecobCode,
        Dictionary<string, (string MasterCode, string MasterName)> directMappings,
        Dictionary<string, string?> digecobParents)
    {
        var currCode = digecobCode?.Trim();
        bool isInherited = false;
        string? inheritedFrom = null;

        while (!string.IsNullOrEmpty(currCode))
        {
            if (directMappings.TryGetValue(currCode, out var match))
            {
                return new MastersoftMappingResolution
                {
                    MastersoftCode = match.MasterCode,
                    MastersoftName = match.MasterName,
                    IsInherited = isInherited,
                    InheritedFromDigecobCode = inheritedFrom
                };
            }

            if (!digecobParents.TryGetValue(currCode, out var parentCode) || string.IsNullOrEmpty(parentCode))
            {
                var lastDot = currCode.LastIndexOf('.');
                if (lastDot > 0)
                {
                    parentCode = currCode.Substring(0, lastDot);
                }
                else
                {
                    break;
                }
            }

            if (!isInherited)
            {
                isInherited = true;
                inheritedFrom = parentCode;
            }
            currCode = parentCode;
        }

        return new MastersoftMappingResolution();
    }

    public class MastersoftMappingResolution
    {
        public string? MastersoftCode { get; set; }
        public string? MastersoftName { get; set; }
        public bool IsInherited { get; set; }
        public string? InheritedFromDigecobCode { get; set; }
    }
}
