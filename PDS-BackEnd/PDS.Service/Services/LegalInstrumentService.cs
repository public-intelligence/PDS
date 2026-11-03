using AutoMapper;
using Microsoft.Extensions.Logging;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.LegalInstrument;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services;

public class LegalInstrumentService(
    ILogger<LegalInstrumentService> logger,
    IMapper mapper,
    IRepository<LegalInstrument> legalInstrumentRepository,
    IRepository<StructureVersion> versionRepository
) : ILegalInstrumentService
{
    private readonly ILogger<LegalInstrumentService> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly IRepository<LegalInstrument> _legalInstrumentRepository = legalInstrumentRepository;
    private readonly IRepository<StructureVersion> _versionRepository = versionRepository;

    public async Task<IHttpResult<IEnumerable<LegalInstrumentDto>>> ListAsync(int organizationId)
    {
        var result = await _legalInstrumentRepository.FilterAsync(li => li.OrganizationId == organizationId);
        if (!result.IsSuccess)
            return FailList("No se pudieron listar los instrumentos legales");

        var dtos = _mapper.Map<IEnumerable<LegalInstrumentDto>>(result.Value);
        return HttpResult<IEnumerable<LegalInstrumentDto>>.Success(dtos);
    }

    public async Task<IHttpResult<LegalInstrumentDto>> GetAsync(int organizationId, Guid id)
    {
        var result = await _legalInstrumentRepository.GetAsync(li => li.OrganizationId == organizationId && li.Id == id);
        if (!result.IsSuccess)
            return Fail("No se pudo recuperar el instrumento legal");
        if (result.Value is null)
            return Fail("El instrumento legal no existe", HttpStatusCode.NotFound);

        return HttpResult<LegalInstrumentDto>.Success(_mapper.Map<LegalInstrumentDto>(result.Value));
    }

    public async Task<IHttpResult<LegalInstrumentDto>> CreateAsync(int organizationId, CreateLegalInstrumentDto dto)
    {
        var entity = _mapper.Map<LegalInstrument>(dto);
        entity.OrganizationId = organizationId;

        var result = await _legalInstrumentRepository.CreateAsync(entity);
        if (!result.IsSuccess)
            return Fail("No se pudo crear el instrumento legal");

        return HttpResult<LegalInstrumentDto>.Success(_mapper.Map<LegalInstrumentDto>(result.Value), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<LegalInstrumentDto>> UpdateAsync(int organizationId, Guid id, UpdateLegalInstrumentDto dto)
    {
        var existingResult = await _legalInstrumentRepository.GetAsync(li => li.OrganizationId == organizationId && li.Id == id);
        if (!existingResult.IsSuccess)
            return Fail("No se pudo recuperar el instrumento legal");
        var existing = existingResult.Value;
        if (existing is null)
            return Fail("El instrumento legal no existe", HttpStatusCode.NotFound);

        existing.Type = dto.Type;
        existing.Number = dto.Number;
        existing.Date = dto.Date;
        existing.IssuingAuthority = dto.IssuingAuthority;
        existing.DocumentUrl = dto.DocumentUrl;

        var updateResult = await _legalInstrumentRepository.UpdateAsync(existing);
        if (!updateResult.IsSuccess)
            return Fail("No se pudo actualizar el instrumento legal");

        return HttpResult<LegalInstrumentDto>.Success(_mapper.Map<LegalInstrumentDto>(updateResult.Value));
    }

    public async Task<IHttpResult> DeleteAsync(int organizationId, Guid id)
    {
        // No se permite eliminar un instrumento legal referenciado por una versión no rechazada.
        var referencedResult = await _versionRepository.AnyAsync(
            v => v.OrganizationId == organizationId && v.LegalInstrumentId == id && v.Status != VersionStatus.Rejected);
        if (referencedResult.IsSuccess && referencedResult.Value)
        {
            var detail = new Dictionary<string, IEnumerable<string>>
            {
                { "errors", ["No se puede eliminar: el instrumento legal está asociado a una versión."] }
            };
            return HttpResult.Failure("No se puede eliminar el instrumento legal", detail, HttpStatusCode.Conflict);
        }

        var deleteResult = await _legalInstrumentRepository.DeleteAsync(li => li.OrganizationId == organizationId && li.Id == id);
        if (!deleteResult.IsSuccess)
        {
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo eliminar el instrumento legal"] } };
            return HttpResult.Failure("Error al eliminar el instrumento legal", detail, HttpStatusCode.BadRequest);
        }
        return HttpResult.Success();
    }

    private static HttpResult<LegalInstrumentDto> Fail(string message, HttpStatusCode code = HttpStatusCode.BadRequest)
    {
        var detail = new Dictionary<string, IEnumerable<string>> { { "errors", [message] } };
        return HttpResult<LegalInstrumentDto>.Failure("Error con el instrumento legal", detail, code);
    }

    private static HttpResult<IEnumerable<LegalInstrumentDto>> FailList(string message)
    {
        var detail = new Dictionary<string, IEnumerable<string>> { { "errors", [message] } };
        return HttpResult<IEnumerable<LegalInstrumentDto>>.Failure("Error con el instrumento legal", detail, HttpStatusCode.BadRequest);
    }
}
