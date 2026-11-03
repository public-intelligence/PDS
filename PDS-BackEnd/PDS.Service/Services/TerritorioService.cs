using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Territorio;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services;

public interface ITerritorioService
{
    Task<IHttpResult<IEnumerable<PaisDto>>> GetPaisesAsync();
    Task<IHttpResult<IEnumerable<RegionDto>>> GetRegionesAsync(Guid? paisId);
    Task<IHttpResult<IEnumerable<ProvinciaDto>>> GetProvinciasAsync(Guid? regionId, Guid? paisId);
    Task<IHttpResult<IEnumerable<MunicipioDto>>> GetMunicipiosAsync(Guid? provinciaId);
    Task<IHttpResult<IEnumerable<DistritoMunicipalDto>>> GetDistritosAsync(Guid municipioId);
}

/// <summary>
/// Lectura del catálogo territorial en cascada (RF-LO): País → Región → Provincia → Municipio → Distrito.
/// </summary>
public class TerritorioService(
    ILogger<TerritorioService> logger,
    IMapper mapper,
    IRepository<Pais> paisRepository,
    IRepository<Region> regionRepository,
    IRepository<Provincia> provinciaRepository,
    IRepository<Municipio> municipioRepository,
    IRepository<DistritoMunicipal> distritoRepository
) : ITerritorioService
{
    private readonly ILogger<TerritorioService> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly IRepository<Pais> _paisRepository = paisRepository;
    private readonly IRepository<Region> _regionRepository = regionRepository;
    private readonly IRepository<Provincia> _provinciaRepository = provinciaRepository;
    private readonly IRepository<Municipio> _municipioRepository = municipioRepository;
    private readonly IRepository<DistritoMunicipal> _distritoRepository = distritoRepository;

    public async Task<IHttpResult<IEnumerable<PaisDto>>> GetPaisesAsync()
    {
        var result = await _paisRepository.FilterAsync(p => true, q => q.OrderBy(p => p.Nombre));
        return Ok(_mapper.Map<IEnumerable<PaisDto>>(result.Value));
    }

    public async Task<IHttpResult<IEnumerable<RegionDto>>> GetRegionesAsync(Guid? paisId)
    {
        var result = await _regionRepository.FilterAsync(
            r => paisId == null || r.PaisId == paisId,
            q => q.OrderBy(r => r.Nombre));
        return Ok(_mapper.Map<IEnumerable<RegionDto>>(result.Value));
    }

    public async Task<IHttpResult<IEnumerable<ProvinciaDto>>> GetProvinciasAsync(Guid? regionId, Guid? paisId)
    {
        var result = await _provinciaRepository.FilterAsync(
            p => (regionId == null || p.RegionId == regionId) && (paisId == null || p.PaisId == paisId),
            q => q.OrderBy(p => p.Nombre));
        return Ok(_mapper.Map<IEnumerable<ProvinciaDto>>(result.Value));
    }

    // M5 (POA): sin provincia devuelve el catálogo completo (selector de municipios de impacto).
    public async Task<IHttpResult<IEnumerable<MunicipioDto>>> GetMunicipiosAsync(Guid? provinciaId)
    {
        var result = await _municipioRepository.FilterAsync(
            m => provinciaId == null || m.ProvinciaId == provinciaId,
            q => q.OrderBy(m => m.Nombre));
        return Ok(_mapper.Map<IEnumerable<MunicipioDto>>(result.Value));
    }

    public async Task<IHttpResult<IEnumerable<DistritoMunicipalDto>>> GetDistritosAsync(Guid municipioId)
    {
        var result = await _distritoRepository.FilterAsync(
            d => d.MunicipioId == municipioId,
            q => q.OrderBy(d => d.Nombre));
        return Ok(_mapper.Map<IEnumerable<DistritoMunicipalDto>>(result.Value));
    }

    private static HttpResult<T> Ok<T>(T value) => (HttpResult<T>)HttpResult<T>.Success(value);
}
