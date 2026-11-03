using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PDS.Application.Repositories;
using PDS.Domain.Models;

namespace PDS.Application.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.TryAddScoped<IRepository<OrganizationalStructure>, SoftDeleteRepository<OrganizationalStructure>>();
        services.TryAddScoped<IRepository<Position>, SoftDeleteRepository<Position>>();
        services.TryAddScoped<IRepository<OccupationalGroup>, SoftDeleteRepository<OccupationalGroup>>();
        services.TryAddScoped<IRepository<OrganizationalStructureAreaType>, SoftDeleteRepository<OrganizationalStructureAreaType>>();

        services.TryAddScoped<IRepository<Position>, SoftDeleteRepository<Position>>();

        // Versionado: las fotografías inmutables usan GenericRepository (sin soft-delete) y nunca
        // se eliminan. El instrumento legal es dato maestro corregible → SoftDeleteRepository.
        services.TryAddScoped<IRepository<StructureVersion>, GenericRepository<StructureVersion>>();
        services.TryAddScoped<IRepository<StructureVersionUnit>, GenericRepository<StructureVersionUnit>>();
        services.TryAddScoped<IRepository<StructureVersionPosition>, GenericRepository<StructureVersionPosition>>();
        services.TryAddScoped<IRepository<StructureVersionAuditLog>, GenericRepository<StructureVersionAuditLog>>();
        services.TryAddScoped<IRepository<LegalInstrument>, SoftDeleteRepository<LegalInstrument>>();
        services.TryAddScoped<IRepository<OrganizationDirectory>, GenericRepository<OrganizationDirectory>>();

        // Localidades institucionales.
        // Catálogos territoriales: datos de referencia, nunca se eliminan → GenericRepository.
        services.TryAddScoped<IRepository<Pais>, GenericRepository<Pais>>();
        services.TryAddScoped<IRepository<Region>, GenericRepository<Region>>();
        services.TryAddScoped<IRepository<Provincia>, GenericRepository<Provincia>>();
        services.TryAddScoped<IRepository<Municipio>, GenericRepository<Municipio>>();
        services.TryAddScoped<IRepository<DistritoMunicipal>, GenericRepository<DistritoMunicipal>>();

        // Catálogos editables y raíz/dependientes con baja lógica → SoftDeleteRepository.
        services.TryAddScoped<IRepository<TipoLocalidad>, SoftDeleteRepository<TipoLocalidad>>();
        services.TryAddScoped<IRepository<NaturalezaAdministrativa>, SoftDeleteRepository<NaturalezaAdministrativa>>();
        services.TryAddScoped<IRepository<EstadoLocalidad>, SoftDeleteRepository<EstadoLocalidad>>();
        services.TryAddScoped<IRepository<Servicio>, SoftDeleteRepository<Servicio>>();
        services.TryAddScoped<IRepository<Responsable>, SoftDeleteRepository<Responsable>>();
        services.TryAddScoped<IRepository<Localidad>, SoftDeleteRepository<Localidad>>();
        services.TryAddScoped<IRepository<LocalidadDocumento>, SoftDeleteRepository<LocalidadDocumento>>();
        services.TryAddScoped<IRepository<LocalidadServicio>, SoftDeleteRepository<LocalidadServicio>>();
        services.TryAddScoped<IRepository<LocalidadResponsable>, SoftDeleteRepository<LocalidadResponsable>>();

        // Dependientes sin baja lógica (se gestionan dentro del agregado) → GenericRepository.
        services.TryAddScoped<IRepository<LocalidadHorarioExcepcion>, GenericRepository<LocalidadHorarioExcepcion>>();
        services.TryAddScoped<IRepository<LocalidadUnidadPresencia>, GenericRepository<LocalidadUnidadPresencia>>();
        services.TryAddScoped<IRepository<LocalidadCoberturaTerritorial>, GenericRepository<LocalidadCoberturaTerritorial>>();
        services.TryAddScoped<IRepository<LocalidadInstrumentoLegal>, GenericRepository<LocalidadInstrumentoLegal>>();
        services.TryAddScoped<IRepository<LocalidadAuditoria>, GenericRepository<LocalidadAuditoria>>();

        // Planificación: bitácora append-only → GenericRepository (nunca se elimina).
        services.TryAddScoped<IRepository<PlanningActivityLog>, GenericRepository<PlanningActivityLog>>();
        services.TryAddScoped<IRepository<PlanningConfigEntry>, GenericRepository<PlanningConfigEntry>>();
        services.TryAddScoped<IRepository<SystemEntryLog>, GenericRepository<SystemEntryLog>>();

        // Planificación (M2): plantillas, matriz de códigos y categorías de apoyo.
        services.TryAddScoped<IRepository<PlanningTemplate>, SoftDeleteRepository<PlanningTemplate>>();
        services.TryAddScoped<IRepository<PlanningTemplateColumn>, SoftDeleteRepository<PlanningTemplateColumn>>();
        services.TryAddScoped<IRepository<UnifiedCode>, SoftDeleteRepository<UnifiedCode>>();
        services.TryAddScoped<IRepository<UnifiedCodeAlias>, GenericRepository<UnifiedCodeAlias>>();

        // M3: PEI — sin soft delete: el único borrado permitido es el descarte de la enmienda
        // Draft (cascada del grafo completo, controlado por el servicio).
        services.TryAddScoped<IRepository<StrategicPlan>, GenericRepository<StrategicPlan>>();
        services.TryAddScoped<IRepository<StrategicFocus>, GenericRepository<StrategicFocus>>();
        services.TryAddScoped<IRepository<StrategicObjective>, GenericRepository<StrategicObjective>>();
        services.TryAddScoped<IRepository<StrategicProduct>, GenericRepository<StrategicProduct>>();
        services.TryAddScoped<IRepository<StrategicProductInvolvedArea>, GenericRepository<StrategicProductInvolvedArea>>();
        services.TryAddScoped<IRepository<StrategicMeasureUnit>, GenericRepository<StrategicMeasureUnit>>();
        services.TryAddScoped<IRepository<StrategicAnnualTarget>, GenericRepository<StrategicAnnualTarget>>();
        services.TryAddScoped<IRepository<StrategicPlanChange>, GenericRepository<StrategicPlanChange>>();

        // M4: Ciclos de planificación.
        services.TryAddScoped<IRepository<PlanningCycle>, SoftDeleteRepository<PlanningCycle>>();
        services.TryAddScoped<IRepository<PlanningCycleMilestone>, GenericRepository<PlanningCycleMilestone>>();
        services.TryAddScoped<IRepository<PlanningCycleArea>, GenericRepository<PlanningCycleArea>>();

        // M5 & M6: POA Workspace, validación y Excel.
        services.TryAddScoped<IRepository<AnnualPlan>, GenericRepository<AnnualPlan>>();
        services.TryAddScoped<IRepository<PoaProduct>, GenericRepository<PoaProduct>>();
        services.TryAddScoped<IRepository<PoaMeasureUnit>, GenericRepository<PoaMeasureUnit>>();
        services.TryAddScoped<IRepository<PoaTask>, GenericRepository<PoaTask>>();
        services.TryAddScoped<IRepository<PoaProductMunicipality>, GenericRepository<PoaProductMunicipality>>();
        services.TryAddScoped<IRepository<PoaProductInvolvedArea>, GenericRepository<PoaProductInvolvedArea>>();
        services.TryAddScoped<IRepository<PoaProductCustomValue>, GenericRepository<PoaProductCustomValue>>();
        services.TryAddScoped<IRepository<PlanningSequence>, GenericRepository<PlanningSequence>>();

        // M7: Requerimientos y Consolidación.
        services.TryAddScoped<IRepository<Requirement>, GenericRepository<Requirement>>();
        services.TryAddScoped<IRepository<RequirementComment>, GenericRepository<RequirementComment>>();
        services.TryAddScoped<IRepository<PrioritizationDecision>, GenericRepository<PrioritizationDecision>>();

        // M8: Maestra de Artículos y Catálogos Regulatorios.
        services.TryAddScoped<IRepository<MasterProduct>, SoftDeleteRepository<MasterProduct>>();
        services.TryAddScoped<IRepository<MasterProductAlias>, GenericRepository<MasterProductAlias>>();
        services.TryAddScoped<IRepository<ExpenseObject>, SoftDeleteRepository<ExpenseObject>>();
        services.TryAddScoped<IRepository<AccountingAccount>, SoftDeleteRepository<AccountingAccount>>();
        services.TryAddScoped<IRepository<ConsolidatorRoutingRule>, SoftDeleteRepository<ConsolidatorRoutingRule>>();
        services.TryAddScoped<IRepository<ConsolidatorAreaProfile>, SoftDeleteRepository<ConsolidatorAreaProfile>>();

        return services;
    }
}
