using AutoMapper;
using PDS.Domain.DTOs.CatalogoLocalidad;
using PDS.Domain.DTOs.LegalInstrument;
using PDS.Domain.DTOs.OccupationalGroups;
using PDS.Domain.DTOs.OrganizationalStructure;
using PDS.Domain.DTOs.OrganizationalStructureAreatype;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.DTOs.Position;
using PDS.Domain.DTOs.Responsable;
using PDS.Domain.DTOs.Servicio;
using PDS.Domain.DTOs.StructureVersion;
using PDS.Domain.DTOs.Territorio;
using PDS.Domain.Models;

namespace PDS.Service.Mappers;

public class MapperProfile : Profile
{
    public MapperProfile()
    {

        CreateMap<CreatePositionDto, Position>();
        CreateMap<Position, PositionDto>();
        CreateMap<UpdatePositionDto, Position>();

        CreateMap<CreateOrganizationalStructureDto, OrganizationalStructure>();
        CreateMap<UpdateOrganizationalStructureDto, OrganizationalStructure>();
        CreateMap<OrganizationalStructure, OrganizationalStructureDto>()
            .ForMember(dest => dest.Parent, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null));
        CreateMap<OrganizationalStructure, ParentOrganizationalStructureDto>();
        CreateMap<OrganizationalStructure, OrganizationalStructureWhioutDependenciesDto>()
            .ForMember(dest => dest.Parent, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null));

        CreateMap<CreateOccupationalGroupDto, OccupationalGroup>();
        CreateMap<OccupationalGroup, OccupationalGroupDto>();
        CreateMap<UpdateOccupationalGroupDto, OccupationalGroup>();

        CreateMap<CreateOrganizationalStructureAreaTypeDto, OrganizationalStructureAreaType>();
        CreateMap<UpdateOrganizationalStructureAreaTypeDto, OrganizationalStructureAreaType>();
        CreateMap<OrganizationalStructureAreaType, OrganizationalStructureAreaTypeDto>();

        // Planificación (M1).
        CreateMap<PlanningActivityLog, ActivityLogDto>();

        // Planificación (M2): plantillas, columnas, códigos unificados, categorías.
        CreateMap<PlanningTemplate, PlanningTemplateDto>()
            .ForMember(dest => dest.ColumnCount, opt => opt.MapFrom(src => src.Columns.Count(c => c.DeletedAt == null)));
        CreateMap<PlanningTemplate, PlanningTemplateDetailDto>()
            .ForMember(dest => dest.Columns, opt => opt.MapFrom(src =>
                src.Columns.Where(c => c.DeletedAt == null).OrderBy(c => c.Order)));
        CreateMap<PlanningTemplateColumn, PlanningTemplateColumnDto>();
        CreateMap<UnifiedCode, UnifiedCodeDto>()
            .ForMember(dest => dest.ChildCount, opt => opt.MapFrom(src => src.Children.Count(c => c.DeletedAt == null)));
        CreateMap<UnifiedCodeAlias, UnifiedCodeAliasDto>();

        // Versionado, historial y organigrama.
        CreateMap<CreateLegalInstrumentDto, LegalInstrument>();
        CreateMap<UpdateLegalInstrumentDto, LegalInstrument>();
        CreateMap<LegalInstrument, LegalInstrumentDto>();

        CreateMap<StructureVersion, StructureVersionSummaryDto>();
        CreateMap<StructureVersion, StructureVersionDetailDto>();
        CreateMap<StructureVersionAuditLog, AuditLogDto>();

        // ── Localidades institucionales ──
        // Catálogos territoriales (solo lectura).
        CreateMap<Pais, PaisDto>();
        CreateMap<Region, RegionDto>();
        CreateMap<Provincia, ProvinciaDto>();
        CreateMap<Municipio, MunicipioDto>();
        CreateMap<DistritoMunicipal, DistritoMunicipalDto>();

        // Catálogos de clasificación.
        CreateMap<TipoLocalidad, TipoLocalidadDto>();
        CreateMap<CreateTipoLocalidadDto, TipoLocalidad>();
        CreateMap<UpdateTipoLocalidadDto, TipoLocalidad>();
        CreateMap<NaturalezaAdministrativa, NaturalezaAdministrativaDto>();
        CreateMap<CreateNaturalezaAdministrativaDto, NaturalezaAdministrativa>();
        CreateMap<UpdateNaturalezaAdministrativaDto, NaturalezaAdministrativa>();
        CreateMap<EstadoLocalidad, EstadoLocalidadDto>();
        CreateMap<CreateEstadoLocalidadDto, EstadoLocalidad>();
        CreateMap<UpdateEstadoLocalidadDto, EstadoLocalidad>();

        // Catálogos por organización.
        CreateMap<Servicio, ServicioDto>();
        CreateMap<CreateServicioDto, Servicio>();
        CreateMap<UpdateServicioDto, Servicio>();
        CreateMap<Responsable, ResponsableDto>();
        // RF-LO-37: el vínculo GobID lo valida y asigna el servicio (permiso, formato y unicidad).
        CreateMap<CreateResponsableDto, Responsable>().ForMember(r => r.GobidUserId, o => o.Ignore());
        CreateMap<UpdateResponsableDto, Responsable>().ForMember(r => r.GobidUserId, o => o.Ignore());
        // El mapeo de Localidad y sus hijos se hace de forma manual en LocalidadService.
    }
}
