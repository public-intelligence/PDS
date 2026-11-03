using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PDS.Service.Authorization;
using PDS.Service.Services;
using PDS.Service.Services.Calendario;
using PDS.Service.Services.Calendario.Motor;
using PDS.Service.Services.Planning;
using PDS.Service.Services.Reporting;

public static class ServiceExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.TryAddScoped<IOrganizationalStructureService, OrganizationalStructureService>();
        services.TryAddScoped<IPaginationService, PaginationService>();
        services.TryAddScoped<IOrganizationalStructureAreaTypeService, OrganizationalStructureAreaTypeService>();
        services.TryAddScoped<IOccupationalGroupService, OccupationalGroupService>();
        services.TryAddScoped<IPositionService, PositionService>();

        // Versionado, historial y organigrama.
        services.TryAddScoped<IStructureVersionService, StructureVersionService>();
        services.TryAddScoped<IOrgChartService, OrgChartService>();
        services.TryAddScoped<ILegalInstrumentService, LegalInstrumentService>();
        services.TryAddScoped<IOrganizationDirectoryService, OrganizationDirectoryService>();

        // Reportería e indicadores (RF-RE-01..07). Solo lectura sobre la estructura publicada.
        services.TryAddScoped<IReportingContextResolver, ReportingContextResolver>();
        services.TryAddScoped<IAuthorizedPostsReportService, AuthorizedPostsReportService>();
        services.TryAddScoped<IStaffingComplementReportService, StaffingComplementReportService>();
        services.TryAddScoped<IEstimatedCostReportService, EstimatedCostReportService>();
        services.TryAddScoped<IComparativeReportService, ComparativeReportService>();
        services.TryAddScoped<IStructuralChangesReportService, StructuralChangesReportService>();
        services.TryAddScoped<IDashboardService, DashboardService>();
        services.TryAddScoped<IReportExportService, ReportExportService>();
        // Renderers: AddScoped (no TryAdd) para que IEnumerable<IReportRenderer> resuelva los tres.
        services.AddScoped<IReportRenderer, PdfReportRenderer>();
        services.AddScoped<IReportRenderer, XlsxReportRenderer>();
        services.AddScoped<IReportRenderer, CsvReportRenderer>();

        // Localidades institucionales.
        services.TryAddScoped<ITerritorioService, TerritorioService>();
        services.TryAddScoped<ITipoLocalidadService, TipoLocalidadService>();
        services.TryAddScoped<IFuncionLocalidadService, FuncionLocalidadService>();
        services.TryAddScoped<IZonaHorariaService, ZonaHorariaService>();
        services.TryAddScoped<ICalendarioCambioService, CalendarioCambioService>();
        services.AddHostedService<CalendarioCambiosPurgaService>();   // RF-LO-43: retención del feed
        services.TryAddScoped<ICalendarioAuditoriaService, CalendarioAuditoriaService>();
        services.TryAddScoped<IRetroactiveWindowPolicy, RetroactiveWindowPolicy>();
        services.TryAddScoped<ICalendarioFeriadosService, CalendarioFeriadosService>();
        services.TryAddScoped<ICalendarioLaboralService, CalendarioLaboralService>();
        services.TryAddScoped<ICalendarioExcepcionesService, CalendarioExcepcionesService>();
        services.TryAddSingleton<ICalendarioEfectivoResolver, CalendarioEfectivoResolver>();
        services.TryAddSingleton<ITiempoLaborableCalculator, TiempoLaborableCalculator>();
        services.AddMemoryCache();
        services.TryAddScoped<ICalendarioSnapshotService, CalendarioSnapshotService>();
        services.TryAddScoped<ICalendarioLecturaService, CalendarioLecturaService>();
        services.TryAddScoped<ITiempoLaborableService, TiempoLaborableService>();
        services.TryAddScoped<INaturalezaAdministrativaService, NaturalezaAdministrativaService>();
        services.TryAddScoped<IEstadoLocalidadService, EstadoLocalidadService>();
        services.TryAddScoped<IServicioService, ServicioService>();
        services.TryAddScoped<IResponsableService, ResponsableService>();
        services.TryAddScoped<ILocalidadService, LocalidadService>();

        // Planificación (M1): contexto de usuario, permisos y bitácora.
        // El cliente GOBID Personnel se registra en Program.cs (HttpClient tipado + bearer M2M).
        services.TryAddScoped<IPlanningContextService, PlanningContextService>();
        services.TryAddSingleton<IPlanningAccessGuard, PlanningAccessGuard>();
        services.TryAddScoped<ICalendarioAccessGuard, CalendarioAccessGuard>(); // consulta la base (RF-LO-38)
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IPlanningActivityService, PlanningActivityService>();
        services.AddHttpContextAccessor();
        services.TryAddScoped<IPlanningConfigService, PlanningConfigService>();

        // Planificación (M2): plantillas del plan anual, matriz de códigos y categorías de apoyo.
        services.TryAddScoped<IPlanningTemplateService, PlanningTemplateService>();
        services.TryAddScoped<IUnifiedCodeService, UnifiedCodeService>();

        // M3: PEI versionado por enmiendas + Excel.
        services.TryAddScoped<IStrategicPlanService, StrategicPlanService>();
        services.TryAddScoped<IStrategicPlanExcelService, StrategicPlanExcelService>();

        // M4: Ciclos de planificación y publicación.
        services.TryAddScoped<IPlanningCycleService, PlanningCycleService>();

        // M5: Plan anual (POA) — workspace, validación central y secuencias de códigos.
        services.TryAddScoped<ISequenceService, SequenceService>();
        services.TryAddSingleton<IPoaValidationService, PoaValidationService>();
        services.TryAddScoped<IAnnualPlanService, AnnualPlanService>();

        // M6: Excel del POA (plantilla, import por área, export institucional).
        services.TryAddScoped<IPoaExcelService, PoaExcelService>();

        // M7: Requerimientos y Consolidación.
        services.TryAddScoped<IRequirementService, RequirementService>();
        services.TryAddScoped<IConsolidationService, ConsolidationService>();

        // M8/M9: Maestra de Artículos y Catálogos Regulatorios.
        services.TryAddScoped<IMasterProductService, MasterProductService>();
        services.TryAddScoped<IConsolidatorRoutingService, ConsolidatorRoutingService>();
        services.TryAddScoped<IConsolidatorImpactService, ConsolidatorImpactService>();
        services.TryAddScoped<IMasterDataImportService, MasterDataImportService>();
        services.TryAddScoped<IRegulatoryCatalogService, RegulatoryCatalogService>();
        // M10: Techos Presupuestarios y Catálogo de Fuentes.
        services.TryAddScoped<IBudgetCeilingService, BudgetCeilingService>();
        services.TryAddScoped<IFundingSourceService, FundingSourceService>();
        return services;
    }
}
