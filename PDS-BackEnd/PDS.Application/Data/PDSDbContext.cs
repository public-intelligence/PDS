using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PDS.Domain.Models;

namespace PDS.Application.Data;

public class PDSDbContext : DbContext
{
    public DbSet<OrganizationalStructure> OrganizationalStructures => Set<OrganizationalStructure>();
    public DbSet<OrganizationalStructureAreaType> OrganizationalStructureAreaTypes => Set<OrganizationalStructureAreaType>();
    public DbSet<OccupationalGroup> OccupationalGroups => Set<OccupationalGroup>();
    public DbSet<Position> Positions => Set<Position>();

    // Versionado e historial de la estructura organizativa.
    public DbSet<StructureVersion> StructureVersions => Set<StructureVersion>();
    public DbSet<StructureVersionUnit> StructureVersionUnits => Set<StructureVersionUnit>();
    public DbSet<StructureVersionPosition> StructureVersionPositions => Set<StructureVersionPosition>();
    public DbSet<LegalInstrument> LegalInstruments => Set<LegalInstrument>();
    public DbSet<StructureVersionAuditLog> StructureVersionAuditLogs => Set<StructureVersionAuditLog>();

    // Directorio mínimo de organizaciones (tenants) para resolver el organigrama público por slug.
    public DbSet<OrganizationDirectory> Organizations => Set<OrganizationDirectory>();

    // ───────────────────────── Localidades institucionales ─────────────────────────
    // Catálogos territoriales (datos de referencia nacional, compartidos).
    public DbSet<Pais> Paises => Set<Pais>();
    public DbSet<Region> Regiones => Set<Region>();
    public DbSet<Provincia> Provincias => Set<Provincia>();
    public DbSet<Municipio> Municipios => Set<Municipio>();
    public DbSet<DistritoMunicipal> DistritosMunicipales => Set<DistritoMunicipal>();

    // Catálogos de clasificación (compartidos) y de gestión (por organización).
    public DbSet<TipoLocalidad> TiposLocalidad => Set<TipoLocalidad>();
    public DbSet<NaturalezaAdministrativa> NaturalezasAdministrativas => Set<NaturalezaAdministrativa>();
    public DbSet<FuncionLocalidad> FuncionesLocalidad => Set<FuncionLocalidad>();
    public DbSet<LocalidadFuncion> LocalidadFunciones => Set<LocalidadFuncion>();
    public DbSet<EstadoLocalidad> EstadosLocalidad => Set<EstadoLocalidad>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Responsable> Responsables => Set<Responsable>();

    // Localidad (raíz) y sus dependientes.
    public DbSet<Localidad> Localidades => Set<Localidad>();
    public DbSet<LocalidadHorarioExcepcion> LocalidadHorariosExcepcion => Set<LocalidadHorarioExcepcion>();
    public DbSet<LocalidadUnidadPresencia> LocalidadUnidadesPresencia => Set<LocalidadUnidadPresencia>();
    public DbSet<LocalidadCoberturaTerritorial> LocalidadCoberturas => Set<LocalidadCoberturaTerritorial>();
    public DbSet<LocalidadInstrumentoLegal> LocalidadInstrumentosLegales => Set<LocalidadInstrumentoLegal>();
    public DbSet<LocalidadDocumento> LocalidadDocumentos => Set<LocalidadDocumento>();
    public DbSet<LocalidadServicio> LocalidadServicios => Set<LocalidadServicio>();
    public DbSet<LocalidadResponsable> LocalidadResponsables => Set<LocalidadResponsable>();
    public DbSet<LocalidadAuditoria> LocalidadAuditorias => Set<LocalidadAuditoria>();

    // Calendario laboral de localidades (SPEC-LOC-CAL-01 §9.3).
    public DbSet<LocalidadJornada> LocalidadJornadas => Set<LocalidadJornada>();
    public DbSet<CalendarioFeriados> CalendariosFeriados => Set<CalendarioFeriados>();
    public DbSet<CalendarioFeriadosDia> CalendarioFeriadosDias => Set<CalendarioFeriadosDia>();
    public DbSet<LocalidadCalendarioFeriados> LocalidadCalendariosFeriados => Set<LocalidadCalendarioFeriados>();
    public DbSet<LocalidadExcepcionCalendario> LocalidadExcepcionesCalendario => Set<LocalidadExcepcionCalendario>();
    public DbSet<CalendarioCambio> CalendarioCambios => Set<CalendarioCambio>();
    public DbSet<CalendarioFeriadosAuditoria> CalendarioFeriadosAuditorias => Set<CalendarioFeriadosAuditoria>();

    // ───────────────────────── Planificación (PDS) ─────────────────────────
    public DbSet<PlanningActivityLog> PlanningActivityLogs => Set<PlanningActivityLog>();
    public DbSet<PlanningConfigEntry> PlanningConfigEntries => Set<PlanningConfigEntry>();
    public DbSet<SystemEntryLog> SystemEntryLogs => Set<SystemEntryLog>();
    public DbSet<PlanningTemplate> PlanningTemplates => Set<PlanningTemplate>();
    public DbSet<PlanningTemplateColumn> PlanningTemplateColumns => Set<PlanningTemplateColumn>();
    public DbSet<UnifiedCode> UnifiedCodes => Set<UnifiedCode>();
    public DbSet<UnifiedCodeAlias> UnifiedCodeAliases => Set<UnifiedCodeAlias>();

    // M3: PEI (plan estratégico versionado por enmiendas).
    public DbSet<StrategicPlan> StrategicPlans => Set<StrategicPlan>();
    public DbSet<StrategicFocus> StrategicFocuses => Set<StrategicFocus>();
    public DbSet<StrategicObjective> StrategicObjectives => Set<StrategicObjective>();
    public DbSet<StrategicProduct> StrategicProducts => Set<StrategicProduct>();
    public DbSet<StrategicProductInvolvedArea> StrategicProductInvolvedAreas => Set<StrategicProductInvolvedArea>();
    public DbSet<StrategicMeasureUnit> StrategicMeasureUnits => Set<StrategicMeasureUnit>();
    public DbSet<StrategicAnnualTarget> StrategicAnnualTargets => Set<StrategicAnnualTarget>();
    public DbSet<StrategicPlanChange> StrategicPlanChanges => Set<StrategicPlanChange>();

    // M4: Ciclos de planificación y Planes Anuales (POA).
    public DbSet<PlanningCycle> PlanningCycles => Set<PlanningCycle>();
    public DbSet<PlanningCycleMilestone> PlanningCycleMilestones => Set<PlanningCycleMilestone>();
    public DbSet<PlanningCycleArea> PlanningCycleAreas => Set<PlanningCycleArea>();
    public DbSet<AnnualPlan> AnnualPlans => Set<AnnualPlan>();
    public DbSet<PoaProduct> PoaProducts => Set<PoaProduct>();
    public DbSet<PoaMeasureUnit> PoaMeasureUnits => Set<PoaMeasureUnit>();
    public DbSet<PoaTask> PoaTasks => Set<PoaTask>();
    public DbSet<PoaProductMunicipality> PoaProductMunicipalities => Set<PoaProductMunicipality>();
    public DbSet<PoaProductInvolvedArea> PoaProductInvolvedAreas => Set<PoaProductInvolvedArea>();
    public DbSet<PoaProductCustomValue> PoaProductCustomValues => Set<PoaProductCustomValue>();
    public DbSet<PlanningSequence> PlanningSequences => Set<PlanningSequence>();

    // M7: Requerimientos y Consolidación.
    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<RequirementComment> RequirementComments => Set<RequirementComment>();
    public DbSet<PrioritizationDecision> PrioritizationDecisions => Set<PrioritizationDecision>();

    // M8/M9: Maestra de Artículos y Catálogos Regulatorios.
    public DbSet<MasterProduct> MasterProducts => Set<MasterProduct>();
    public DbSet<MasterProductAlias> MasterProductAliases => Set<MasterProductAlias>();
    public DbSet<ExpenseObject> ExpenseObjects => Set<ExpenseObject>();
    public DbSet<AccountingAccount> AccountingAccounts => Set<AccountingAccount>();
    public DbSet<MastersoftAccount> MastersoftAccounts => Set<MastersoftAccount>();
    public DbSet<MastersoftDigecobMapping> MastersoftDigecobMappings => Set<MastersoftDigecobMapping>();
    public DbSet<ConsolidatorRoutingRule> ConsolidatorRoutingRules => Set<ConsolidatorRoutingRule>();
    public DbSet<ConsolidatorAreaProfile> ConsolidatorAreaProfiles => Set<ConsolidatorAreaProfile>();
    public DbSet<BudgetCeiling> BudgetCeilings => Set<BudgetCeiling>();
    public DbSet<BudgetAdjustment> BudgetAdjustments => Set<BudgetAdjustment>();
    public DbSet<PlanningCycleAmendmentLog> PlanningCycleAmendmentLogs => Set<PlanningCycleAmendmentLog>();
    public DbSet<FundingSource> FundingSources => Set<FundingSource>();

    public PDSDbContext(DbContextOptions<PDSDbContext> options)
      : base(options) { }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        TouchModifiedLocalidades();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        TouchModifiedLocalidades();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// <see cref="Localidad.UpdatedAt"/> alimenta la versión/ETag que consumen los sistemas M2M (SDS): se renueva en
    /// cada modificación de la localidad para que sus cachés detecten el cambio.
    /// </summary>
    private void TouchModifiedLocalidades()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Localidad>().Where(e => e.State == EntityState.Modified))
        {
            // RF-LO-43: subir solo CalendarioVersion no cambia los datos de la localidad; no debe mover su versión/ETag M2M.
            var soloCalendarioVersion = entry.Properties
                .Where(p => p.IsModified)
                .All(p => p.Metadata.Name == nameof(Localidad.CalendarioVersion));
            if (soloCalendarioVersion) continue;
            entry.Entity.UpdatedAt = now;
        }
    }

    /// <summary>
    /// Calendario laboral de localidades (SPEC-LOC-CAL-01 §9): tablas nuevas y columnas añadidas a Localidades y
    /// LocalidadAuditorias. Marcas de tiempo en UTC (SYSUTCDATETIME); los intervalos se guardan como JSON (owned types
    /// con ToJson()).
    /// </summary>
    private static void ConfigureCalendarioLaboral(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Localidad>(entity =>
        {
            // RF-LO-43: token de concurrencia, para que dos escritores no emitan la misma versión del calendario.
            entity.Property(e => e.CalendarioVersion).HasDefaultValue(1L).IsConcurrencyToken();
            // RF-LO-36: a lo sumo una localidad predeterminada del calendario por organización.
            entity.Property(e => e.EsLocalidadCalendarioPredeterminada).HasDefaultValue(false);
            entity.HasIndex(e => e.OrganizationId).IsUnique().HasDatabaseName("UX_Localidades_Org_CalendarioPredeterminada")
                .HasFilter("[EsLocalidadCalendarioPredeterminada] = 1 AND [DeletedAt] IS NULL");
        });

        // RF-LO-44: canal del cambio (PdsUi | ApiExterna | Sistema | Migracion).
        modelBuilder.Entity<LocalidadAuditoria>(entity =>
            entity.Property(e => e.Canal).HasConversion<string>().HasMaxLength(20));

        modelBuilder.Entity<LocalidadJornada>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LocalidadJornadas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Motivo).HasMaxLength(1000);
            entity.Property(e => e.CreadoPor).HasMaxLength(250);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.LocalidadId, e.VigenteDesde }).IsUnique();
            entity.HasOne(e => e.Localidad).WithMany().HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
            entity.OwnsMany(e => e.Semana, semana =>
            {
                semana.ToJson("Semana");
                semana.Property(d => d.Dia).HasJsonPropertyName("dia");
                semana.OwnsMany(d => d.Intervalos, iv =>
                {
                    iv.Property(i => i.InicioMinuto).HasJsonPropertyName("inicioMinuto");
                    iv.Property(i => i.FinMinuto).HasJsonPropertyName("finMinuto");
                });
            });
        });

        modelBuilder.Entity<CalendarioFeriados>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "CalendariosFeriados");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Ambito).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => new { e.OrganizationId, e.Codigo }).IsUnique().HasFilter("[DeletedAt] IS NULL");
            entity.HasOne(e => e.Pais).WithMany().HasForeignKey(e => e.PaisId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LocalidadGestora).WithMany().HasForeignKey(e => e.LocalidadGestoraId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CalendarioFeriadosDia>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "CalendarioFeriadosDias");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.Property(e => e.Alcance).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.ReferenciaNormativa).HasMaxLength(500);
            entity.Property(e => e.Notas).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(e => e.RowVersion).IsRowVersion();
            // Una fecha por calendario entre las no eliminadas (duplicado => 409 en el servicio).
            entity.HasIndex(e => new { e.CalendarioFeriadosId, e.Fecha }).IsUnique().HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => e.Fecha);
            entity.HasOne(e => e.CalendarioFeriados).WithMany(c => c.Dias).HasForeignKey(e => e.CalendarioFeriadosId).OnDelete(DeleteBehavior.Restrict);
            entity.OwnsMany(e => e.Intervalos, iv =>
            {
                iv.ToJson("Intervalos");
                iv.Property(i => i.InicioMinuto).HasJsonPropertyName("inicioMinuto");
                iv.Property(i => i.FinMinuto).HasJsonPropertyName("finMinuto");
            });
        });

        modelBuilder.Entity<LocalidadCalendarioFeriados>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LocalidadCalendariosFeriados");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.CreadoPor).HasMaxLength(250);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.LocalidadId, e.CalendarioFeriadosId }).IsUnique();
            entity.HasIndex(e => e.CalendarioFeriadosId);
            entity.HasOne(e => e.Localidad).WithMany().HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CalendarioFeriados).WithMany().HasForeignKey(e => e.CalendarioFeriadosId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LocalidadExcepcionCalendario>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LocalidadExcepcionesCalendario");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Categoria).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Origen).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Motivo).HasMaxLength(1000);
            entity.Property(e => e.ReferenciaNormativa).HasMaxLength(500);
            entity.Property(e => e.CreadoPor).HasMaxLength(250);
            entity.Property(e => e.ModificadoPor).HasMaxLength(250);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => new { e.LocalidadId, e.FechaInicio, e.FechaFin }).HasFilter("[DeletedAt] IS NULL");
            entity.HasOne(e => e.Localidad).WithMany().HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InstrumentoLegal).WithMany().HasForeignKey(e => e.InstrumentoLegalId).OnDelete(DeleteBehavior.Restrict);
            entity.OwnsMany(e => e.Intervalos, iv =>
            {
                iv.ToJson("Intervalos");
                iv.Property(i => i.InicioMinuto).HasJsonPropertyName("inicioMinuto");
                iv.Property(i => i.FinMinuto).HasJsonPropertyName("finMinuto");
            });
        });

        modelBuilder.Entity<CalendarioCambio>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "CalendarioCambios");
            entity.HasKey(e => e.Id);
            // El Id (identity bigint) es el cursor del feed.
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Motivo).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.OcurridoEn).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.OrganizationId, e.Id });
            entity.HasIndex(e => e.LocalidadId);
            entity.HasOne(e => e.Localidad).WithMany().HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CalendarioFeriadosAuditoria>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "CalendarioFeriadosAuditorias");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Accion).HasMaxLength(40);
            entity.Property(e => e.Motivo).HasMaxLength(1000);
            entity.Property(e => e.Usuario).HasMaxLength(250);
            entity.Property(e => e.Canal).HasConversion<string>().HasMaxLength(20);
            // Detalle queda como nvarchar(max) (diff JSON).
            entity.Property(e => e.Fecha).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.CalendarioFeriadosId);
            entity.HasOne(e => e.CalendarioFeriados).WithMany().HasForeignKey(e => e.CalendarioFeriadosId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);
        modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AI");

        modelBuilder.Entity<OrganizationalStructure>(entity => 
        {
            entity.ToTable(schema: "Organization", name: "OrganizationalStructures");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL"); // Soft-delete index filter.

            entity
                .HasOne(e => e.Parent)
                .WithMany(e => e.Dependencies)
                .HasForeignKey(e => e.ParentId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AreaType)
                .WithMany(e => e.OrganizationalStructures)
                .HasForeignKey(e => e.AreaTypeId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(250);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity
                .Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()")
                .ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<OrganizationalStructureAreaType>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "OrganizationalStructureAreaTypes");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL"); // Soft-delete index filter.
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(50);
            entity
                .HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Order);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<OccupationalGroup>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "OccupationalGroups");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Name).IsUnique();

            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL"); // Soft-delete index filter.

            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity
                .Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()")
                .ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "Positions");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL"); // Soft-delete index filter.

            entity
                .HasOne(e => e.OrganizationalStructure)
                .WithMany(e => e.Positions)
                .HasForeignKey(e => e.OrganizationalStructureId)
                .HasPrincipalKey(e => e.Id);

            entity
                .HasOne(e => e.OccupationalGroup)
                .WithMany(e => e.Positions)
                .HasForeignKey(e => e.OccupationalGroupId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Competencies).HasColumnType("TEXT");
            entity.Property(e => e.MinSalary).HasPrecision(18, 2);
            entity.Property(e => e.MaxSalary).HasPrecision(18, 2);
            entity.Property(e => e.YearsOfExperienceRequired);
            entity.Property(e => e.NatureOfWork).HasColumnType("TEXT");
            entity.Property(e => e.TypicalTasks).HasColumnType("TEXT");
            entity.Property(e => e.RequiredEducation).HasColumnType("TEXT");
            entity.Property(e => e.Characteristics).HasColumnType("TEXT");
            entity.Property(e => e.OtherDesirableRequirements).HasColumnType("TEXT");
            entity.Property(e => e.ProbatoryPeriod);
            entity.Property(e => e.Punctuation);
            entity.Property(e => e.Grade);
            entity.Property(e => e.Code);
            entity.Property(e => e.IsManager).HasDefaultValue(false);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity
                .Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()")
                .ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<OrganizationDirectory>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "Organizations");

            entity.HasKey(e => e.Id);
            // El Id proviene del tenant (claim organization_id), no es identidad autogenerada.
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Acronym).HasMaxLength(50);
            entity.Property(e => e.Slug).HasMaxLength(120);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        // ───────────────────────── Versionado e historial ─────────────────────────

        modelBuilder.Entity<StructureVersion>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "StructureVersions");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.EffectiveFrom).HasColumnType("date");
            entity.Property(e => e.EffectiveTo).HasColumnType("date");
            entity.Property(e => e.CreatedByName).HasMaxLength(250);
            entity.Property(e => e.ReviewedByName).HasMaxLength(250);
            entity.Property(e => e.ApprovedByName).HasMaxLength(250);
            entity.Property(e => e.PublishedByName).HasMaxLength(250);

            entity.HasOne(e => e.LegalInstrument)
                .WithMany()
                .HasForeignKey(e => e.LegalInstrumentId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Una sola versión en borrador por organización. (Se usa el overload con nombre para
            // poder definir dos índices filtrados distintos sobre la misma columna OrganizationId.)
            entity.HasIndex(e => e.OrganizationId, "UX_StructureVersions_OneDraftPerOrg")
                .HasFilter("[Status] = 'Draft'")
                .IsUnique();

            // A lo sumo una versión publicada con período abierto (sin solapamiento).
            entity.HasIndex(e => e.OrganizationId, "UX_StructureVersions_OneOpenPublishedPerOrg")
                .HasFilter("[Status] = 'Published' AND [EffectiveTo] IS NULL")
                .IsUnique();

            // Número secuencial único por organización (una vez asignado).
            entity.HasIndex(e => new { e.OrganizationId, e.Number })
                .HasFilter("[Number] IS NOT NULL")
                .IsUnique()
                .HasDatabaseName("UX_StructureVersions_NumberPerOrg");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<StructureVersionUnit>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "StructureVersionUnits");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.AreaTypeName).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(e => e.StructureVersion)
                .WithMany(v => v.Units)
                .HasForeignKey(e => e.StructureVersionId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.StructureVersionId, e.SourceOrganizationalStructureId }).IsUnique();
            entity.HasIndex(e => new { e.StructureVersionId, e.ParentSourceUnitId });
        });

        modelBuilder.Entity<StructureVersionPosition>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "StructureVersionPositions");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.PositionName).HasMaxLength(255);
            entity.Property(e => e.OccupationalGroupName).HasMaxLength(100);
            entity.Property(e => e.AuthorizedPosts).HasDefaultValue(1);

            // RF-RE-03/04: salario y grado fotografiados al publicar (decimal(18,2) como en Position).
            entity.Property(e => e.MinSalary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxSalary).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.StructureVersion)
                .WithMany(v => v.Positions)
                .HasForeignKey(e => e.StructureVersionId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.StructureVersionId, e.SourceOrganizationalStructureId });
        });

        modelBuilder.Entity<LegalInstrument>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LegalInstruments");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL"); // Soft-delete index filter.

            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Number).HasMaxLength(100);
            entity.Property(e => e.Date).HasColumnType("date");
            entity.Property(e => e.IssuingAuthority).HasMaxLength(250);
            entity.Property(e => e.DocumentUrl).HasMaxLength(1000);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<StructureVersionAuditLog>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "StructureVersionAuditLogs");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Action).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.UserName).HasMaxLength(250);
            entity.Property(e => e.Comment).HasMaxLength(2000);

            entity.HasOne(e => e.StructureVersion)
                .WithMany(v => v.AuditLogs)
                .HasForeignKey(e => e.StructureVersionId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.StructureVersionId);
            entity.Property(e => e.Date).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        });

        // ───────────────────────── Localidades institucionales ─────────────────────────
        // Regla crítica: TODAS las FK de este módulo usan DeleteBehavior.Restrict para evitar el
        // error de "múltiples rutas de cascada" de SQL Server (la localidad tiene auto-referencias y
        // varias tablas hijas apuntan de vuelta a ella). Las bajas son lógicas; los hijos se eliminan
        // explícitamente en el servicio.

        // ── Catálogos territoriales (compartidos, sin OrganizationId, sin soft-delete) ──
        modelBuilder.Entity<Pais>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "Paises");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.Property(e => e.CodigoIso3).HasMaxLength(3).IsUnicode(false);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.CodigoIso3).IsUnique().HasFilter("[CodigoIso3] IS NOT NULL");
        });

        modelBuilder.Entity<Region>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "Regiones");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasOne(e => e.Pais).WithMany(p => p.Regiones)
                .HasForeignKey(e => e.PaisId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Provincia>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "Provincias");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasOne(e => e.Pais).WithMany(p => p.Provincias)
                .HasForeignKey(e => e.PaisId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Region).WithMany(r => r.Provincias)
                .HasForeignKey(e => e.RegionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Municipio>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "Municipios");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasOne(e => e.Provincia).WithMany(p => p.Municipios)
                .HasForeignKey(e => e.ProvinciaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DistritoMunicipal>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "DistritosMunicipales");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasOne(e => e.Municipio).WithMany(m => m.DistritosMunicipales)
                .HasForeignKey(e => e.MunicipioId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Catálogos de clasificación (compartidos, soft-delete) ──
        modelBuilder.Entity<TipoLocalidad>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "TiposLocalidad");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => e.Codigo).IsUnique().HasFilter("[DeletedAt] IS NULL");
            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Color).HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        // RF-LO-45 (B30): funciones de localidad (catálogo global, soft-delete) y su relación N:M con las localidades.
        modelBuilder.Entity<FuncionLocalidad>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "FuncionesLocalidad");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => e.Codigo).IsUnique().HasFilter("[DeletedAt] IS NULL");
            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.Property(e => e.Categoria).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            // UpdatedAt lo fija el servicio en cada edición (ValueGeneratedOnAddOrUpdate no lo persiste en UPDATE).
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<LocalidadFuncion>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LocalidadFunciones");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.LocalidadId, e.FuncionLocalidadId }).IsUnique();
            entity.HasOne(e => e.Localidad).WithMany(l => l.Funciones)
                .HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.FuncionLocalidad).WithMany()
                .HasForeignKey(e => e.FuncionLocalidadId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NaturalezaAdministrativa>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "NaturalezasAdministrativas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => e.Codigo).IsUnique().HasFilter("[DeletedAt] IS NULL");
            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<EstadoLocalidad>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "EstadosLocalidad");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => e.Codigo).IsUnique().HasFilter("[DeletedAt] IS NULL");
            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Color).HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        // ── Catálogos por organización (soft-delete) ──
        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "Servicios");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.Nombre }).IsUnique().HasFilter("[DeletedAt] IS NULL");
            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<Responsable>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "Responsables");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => e.OrganizationId);
            entity.Property(e => e.Nombre).HasMaxLength(250);
            entity.Property(e => e.Cargo).HasMaxLength(150);
            entity.Property(e => e.Email).HasMaxLength(250);
            entity.Property(e => e.Telefono).HasMaxLength(50);
            // RF-LO-37 (§9.2): un usuario GobID solo puede estar vinculado a un responsable vigente por organización.
            entity.HasIndex(e => new { e.OrganizationId, e.GobidUserId })
                .IsUnique()
                .HasFilter("[GobidUserId] IS NOT NULL AND [DeletedAt] IS NULL")
                .HasDatabaseName("UX_Responsables_Org_GobidUserId");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        // ── Localidad (raíz) ──
        modelBuilder.Entity<Localidad>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "Localidades");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            // Código único por organización; filtrado en soft-delete para poder reutilizar códigos liberados.
            entity.HasIndex(e => new { e.OrganizationId, e.Codigo }).IsUnique().HasFilter("[DeletedAt] IS NULL");

            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.NombreOficial).HasMaxLength(250);
            entity.Property(e => e.NombreCorto).HasMaxLength(150);
            entity.Property(e => e.Sigla).HasMaxLength(50);
            entity.Property(e => e.Ciudad).HasMaxLength(150);
            entity.Property(e => e.SubdivisionAdministrativa).HasMaxLength(150);
            entity.Property(e => e.SectorBarrioParaje).HasMaxLength(250);
            entity.Property(e => e.DireccionCompleta).HasMaxLength(1000);
            entity.Property(e => e.EdificioPisoLocalReferenciaInterna).HasMaxLength(500);
            entity.Property(e => e.CodigoPostal).HasMaxLength(20);
            entity.Property(e => e.ZonaHoraria).HasMaxLength(64).HasDefaultValue("America/Santo_Domingo");
            entity.Property(e => e.NotasAcceso).HasMaxLength(1000);
            entity.Property(e => e.HorarioRegular).HasMaxLength(1000);
            entity.Property(e => e.SuspensionMotivo).HasMaxLength(1000);
            entity.Property(e => e.CierreMotivo).HasMaxLength(1000);
            // Observaciones, ServiciosAfectados y JustificacionDuplicado quedan como nvarchar(max).

            entity.Property(e => e.FechaApertura).HasColumnType("date");
            entity.Property(e => e.SuspensionFechaInicio).HasColumnType("date");
            entity.Property(e => e.SuspensionFechaFin).HasColumnType("date");
            entity.Property(e => e.CierreFecha).HasColumnType("date");

            // Clasificación
            entity.HasOne(e => e.Tipo).WithMany(t => t.Localidades)
                .HasForeignKey(e => e.TipoLocalidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Naturaleza).WithMany(n => n.Localidades)
                .HasForeignKey(e => e.NaturalezaAdministrativaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Estado).WithMany(s => s.Localidades)
                .HasForeignKey(e => e.EstadoLocalidadId).OnDelete(DeleteBehavior.Restrict);

            // Vínculos con la estructura organizativa (opcionales, sin navegación inversa)
            entity.HasOne(e => e.UnidadResponsable).WithMany()
                .HasForeignKey(e => e.UnidadResponsableId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.UnidadVinculada).WithMany()
                .HasForeignKey(e => e.UnidadVinculadaId).OnDelete(DeleteBehavior.Restrict);

            // Ubicación territorial
            entity.HasOne(e => e.Pais).WithMany()
                .HasForeignKey(e => e.PaisId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Region).WithMany()
                .HasForeignKey(e => e.RegionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Provincia).WithMany()
                .HasForeignKey(e => e.ProvinciaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Municipio).WithMany()
                .HasForeignKey(e => e.MunicipioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DistritoMunicipal).WithMany()
                .HasForeignKey(e => e.DistritoMunicipalId).OnDelete(DeleteBehavior.Restrict);

            // Auto-referencias
            entity.HasOne(e => e.Padre).WithMany(p => p.Hijos)
                .HasForeignKey(e => e.LocalidadPadreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Alterna).WithMany()
                .HasForeignKey(e => e.LocalidadAlternaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Sucesora).WithMany()
                .HasForeignKey(e => e.LocalidadSucesoraId).OnDelete(DeleteBehavior.Restrict);

            // Instrumentos legales de respaldo del ciclo de vida (FK directas, sin navegación)
            entity.HasOne<LegalInstrument>().WithMany()
                .HasForeignKey(e => e.SuspensionInstrumentoLegalId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<LegalInstrument>().WithMany()
                .HasForeignKey(e => e.CierreInstrumentoLegalId).OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate()
                // Sin esto EF ignora UpdatedAt en los UPDATE (columna "generada" sin trigger) y nunca cambiaría;
                // TouchModifiedLocalidades lo renueva y debe persistirse (versión/ETag M2M).
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);
        });

        // ── Dependientes de Localidad ──
        modelBuilder.Entity<LocalidadHorarioExcepcion>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LocalidadHorariosExcepcion");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Horario).HasMaxLength(250);
            entity.Property(e => e.FechaInicio).HasColumnType("date");
            entity.Property(e => e.FechaFin).HasColumnType("date");
            entity.HasOne(e => e.Localidad).WithMany(l => l.HorariosExcepcion)
                .HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LocalidadUnidadPresencia>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LocalidadUnidadesPresencia");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Notas).HasMaxLength(500);
            entity.HasIndex(e => new { e.LocalidadId, e.UnidadOrganizativaId }).IsUnique();
            entity.HasOne(e => e.Localidad).WithMany(l => l.UnidadesPresencia)
                .HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.UnidadOrganizativa).WithMany()
                .HasForeignKey(e => e.UnidadOrganizativaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LocalidadCoberturaTerritorial>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LocalidadCoberturas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.HasOne(e => e.Localidad).WithMany(l => l.Coberturas)
                .HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Region).WithMany()
                .HasForeignKey(e => e.RegionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Provincia).WithMany()
                .HasForeignKey(e => e.ProvinciaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Municipio).WithMany()
                .HasForeignKey(e => e.MunicipioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DistritoMunicipal).WithMany()
                .HasForeignKey(e => e.DistritoMunicipalId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LocalidadInstrumentoLegal>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LocalidadInstrumentosLegales");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Proposito).HasMaxLength(100);
            entity.HasIndex(e => new { e.LocalidadId, e.InstrumentoLegalId, e.Proposito }).IsUnique();
            entity.HasOne(e => e.Localidad).WithMany(l => l.InstrumentosLegales)
                .HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InstrumentoLegal).WithMany()
                .HasForeignKey(e => e.InstrumentoLegalId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LocalidadDocumento>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LocalidadDocumentos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.Property(e => e.TipoDocumento).HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Url).HasMaxLength(1000);
            entity.Property(e => e.RegistradoPor).HasMaxLength(250);
            entity.Property(e => e.StoragePath).HasMaxLength(1000);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
            entity.HasOne(e => e.Localidad).WithMany(l => l.Documentos)
                .HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LocalidadServicio>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LocalidadServicios");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => new { e.LocalidadId, e.ServicioId }).IsUnique().HasFilter("[DeletedAt] IS NULL");
            entity.Property(e => e.FechaHabilitacion).HasColumnType("date");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
            entity.HasOne(e => e.Localidad).WithMany(l => l.Servicios)
                .HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Servicio).WithMany(s => s.LocalidadServicios)
                .HasForeignKey(e => e.ServicioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LocalidadResponsable>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LocalidadResponsables");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.Property(e => e.Rol).HasMaxLength(100);
            entity.Property(e => e.FechaAsignacion).HasColumnType("date");
            entity.Property(e => e.GestionaCalendario).IsRequired().HasDefaultValue(false); // RF-LO-38
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
            entity.HasOne(e => e.Localidad).WithMany(l => l.Responsables)
                .HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Responsable).WithMany(r => r.LocalidadResponsables)
                .HasForeignKey(e => e.ResponsableId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LocalidadAuditoria>(entity =>
        {
            entity.ToTable(schema: "Organization", name: "LocalidadAuditorias");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Accion).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Motivo).HasMaxLength(2000);
            entity.Property(e => e.Usuario).HasMaxLength(250);
            // Detalle queda como nvarchar(max) (diff JSON).
            entity.Property(e => e.Fecha).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.LocalidadId);
            entity.HasOne(e => e.Localidad).WithMany(l => l.Auditorias)
                .HasForeignKey(e => e.LocalidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InstrumentoLegal).WithMany()
                .HasForeignKey(e => e.InstrumentoLegalId).OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureCalendarioLaboral(modelBuilder);

        // ───────────────────────── Planificación (PDS) ─────────────────────────
        // Todas las entidades del módulo de Planificación viven en el esquema "Planning".

        modelBuilder.Entity<PlanningActivityLog>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PlanningActivityLogs");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.UserName).HasMaxLength(250);
            entity.Property(e => e.ActorAreaName).HasMaxLength(250);
            entity.Property(e => e.Module).HasMaxLength(50);
            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.Detail).HasMaxLength(2000);
            entity.Property(e => e.EntityType).HasMaxLength(50);

            entity.Property(e => e.Date).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.OrganizationId, e.Date });
            // EntityType/EntityId son referencia lógica (sin FK): la bitácora sobrevive a la entidad.
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        modelBuilder.Entity<SystemEntryLog>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "SystemEntryLogs");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.UserName).HasMaxLength(250);
            entity.Property(e => e.SessionId).HasMaxLength(100);
            entity.Property(e => e.AreaName).HasMaxLength(250);
            entity.Property(e => e.PositionName).HasMaxLength(250);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.EnteredAtUtc).HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.OrganizationId, e.EnteredAtUtc });
            entity.HasIndex(e => new { e.UserId, e.EnteredAtUtc });
        });

        modelBuilder.Entity<PlanningConfigEntry>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PlanningConfigEntries");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.UpdatedByName).HasMaxLength(250);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();

            // Una sola configuración vigente por organización y clave.
            entity.HasIndex(e => new { e.OrganizationId, e.Key })
                .IsUnique()
                .HasDatabaseName("UX_PlanningConfigEntries_KeyPerOrg");
        });

        // ── M2: Plantillas del plan anual ──
        modelBuilder.Entity<PlanningTemplate>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PlanningTemplates");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.Name })
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL")
                .HasDatabaseName("UX_PlanningTemplates_NamePerOrg");

            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Version).HasDefaultValue(1);
            entity.Property(e => e.IsActive).HasDefaultValue(false);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<PlanningTemplateColumn>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PlanningTemplateColumns");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => new { e.TemplateId, e.TechnicalName })
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL")
                .HasDatabaseName("UX_PlanningTemplateColumns_TechNamePerTemplate");

            entity.Property(e => e.VisibleName).HasMaxLength(150);
            entity.Property(e => e.TechnicalName).HasMaxLength(100);
            entity.Property(e => e.Type).HasMaxLength(20);
            entity.Property(e => e.Origin).HasMaxLength(20);
            entity.Property(e => e.CreatedByName).HasMaxLength(250);
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(e => e.Template)
                .WithMany(t => t.Columns)
                .HasForeignKey(e => e.TemplateId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        });

        // ── M2: Matriz de códigos unificada ──
        modelBuilder.Entity<UnifiedCode>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "UnifiedCodes");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.Code })
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL")
                .HasDatabaseName("UX_UnifiedCodes_CodePerOrg");
            entity.HasIndex(e => new { e.OrganizationId, e.ParentId })
                .HasDatabaseName("IX_UnifiedCodes_OrgParent");

            entity.Property(e => e.Level).HasMaxLength(20);
            entity.Property(e => e.Code).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.InstitutionallyEnabled).HasDefaultValue(true);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<UnifiedCodeAlias>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "UnifiedCodeAliases");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.UnifiedCodeId, e.System, e.Code })
                .IsUnique()
                .HasDatabaseName("UX_UnifiedCodeAliases_SystemCode");

            entity.Property(e => e.System).HasMaxLength(30).IsUnicode(false);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(250);

            entity.HasOne(e => e.UnifiedCode)
                .WithMany(c => c.Aliases)
                .HasForeignKey(e => e.UnifiedCodeId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── M3: PEI (plan estratégico versionado por enmiendas) ──
        // Todo el grafo es UN agregado con árbol estricto (una sola ruta de FK por tabla),
        // por lo que se usa Cascade: descartar la enmienda Draft elimina el grafo completo.
        // La inmutabilidad de Current/Historical la garantiza el servicio, no la BD.

        modelBuilder.Entity<StrategicPlan>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "StrategicPlans");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Reason).HasMaxLength(2000);
            entity.Property(e => e.CreatedByName).HasMaxLength(250);
            entity.Property(e => e.ApprovedByName).HasMaxLength(250);

            // Como máximo una enmienda en elaboración y una versión vigente por organización.
            entity.HasIndex(e => e.OrganizationId)
                .IsUnique()
                .HasFilter("[Status] = 'Draft'")
                .HasDatabaseName("UX_StrategicPlans_OneDraftPerOrg");
            entity.HasIndex(e => e.OrganizationId)
                .IsUnique()
                .HasFilter("[Status] = 'Current'")
                .HasDatabaseName("UX_StrategicPlans_OneCurrentPerOrg");
            entity.HasIndex(e => new { e.OrganizationId, e.VersionNumber })
                .HasDatabaseName("IX_StrategicPlans_OrgVersion");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<StrategicFocus>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "StrategicFocuses");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.EndAlignment).HasMaxLength(500);

            entity.HasIndex(e => new { e.PlanId, e.Order }).HasDatabaseName("IX_StrategicFocuses_PlanOrder");
            entity.HasIndex(e => e.LineageId).HasDatabaseName("IX_StrategicFocuses_Lineage");

            entity.HasOne(e => e.Plan)
                .WithMany(p => p.Focuses)
                .HasForeignKey(e => e.PlanId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<StrategicObjective>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "StrategicObjectives");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(500);

            entity.HasIndex(e => new { e.FocusId, e.Order }).HasDatabaseName("IX_StrategicObjectives_FocusOrder");
            entity.HasIndex(e => e.LineageId).HasDatabaseName("IX_StrategicObjectives_Lineage");

            entity.HasOne(e => e.Focus)
                .WithMany(f => f.Objectives)
                .HasForeignKey(e => e.FocusId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<StrategicProduct>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "StrategicProducts");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.DiscontinuationReason).HasMaxLength(1000);

            entity.HasIndex(e => new { e.ObjectiveId, e.Order }).HasDatabaseName("IX_StrategicProducts_ObjectiveOrder");
            entity.HasIndex(e => e.LineageId).HasDatabaseName("IX_StrategicProducts_Lineage");

            entity.HasOne(e => e.Objective)
                .WithMany(o => o.Products)
                .HasForeignKey(e => e.ObjectiveId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<StrategicProductInvolvedArea>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "StrategicProductInvolvedAreas");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.AreaName).HasMaxLength(250);

            entity.HasIndex(e => new { e.ProductId, e.AreaId })
                .IsUnique()
                .HasDatabaseName("UX_StrategicProductInvolvedAreas_ProductArea");

            entity.HasOne(e => e.Product)
                .WithMany(p => p.InvolvedAreas)
                .HasForeignKey(e => e.ProductId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // AreaId es referencia lógica al organigrama (sin FK dura): el snapshot AreaName
            // preserva el dato histórico ante reestructuraciones.

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<StrategicMeasureUnit>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "StrategicMeasureUnits");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.ResponsibleAreaName).HasMaxLength(250);
            entity.Property(e => e.DiscontinuationReason).HasMaxLength(1000);

            entity.HasIndex(e => new { e.ProductId, e.Order }).HasDatabaseName("IX_StrategicMeasureUnits_ProductOrder");
            entity.HasIndex(e => e.LineageId).HasDatabaseName("IX_StrategicMeasureUnits_Lineage");

            entity.HasOne(e => e.Product)
                .WithMany(p => p.MeasureUnits)
                .HasForeignKey(e => e.ProductId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<StrategicAnnualTarget>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "StrategicAnnualTargets");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Value).HasPrecision(18, 2);

            entity.HasIndex(e => new { e.MeasureUnitId, e.Year })
                .IsUnique()
                .HasDatabaseName("UX_StrategicAnnualTargets_UnitYear");

            entity.HasOne(e => e.MeasureUnit)
                .WithMany(u => u.AnnualTargets)
                .HasForeignKey(e => e.MeasureUnitId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<StrategicPlanChange>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "StrategicPlanChanges");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.UserName).HasMaxLength(250);
            entity.Property(e => e.ChangeType).HasMaxLength(20);
            entity.Property(e => e.Level).HasMaxLength(30);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => new { e.PlanId, e.Date }).HasDatabaseName("IX_StrategicPlanChanges_PlanDate");

            entity.HasOne(e => e.Plan)
                .WithMany(p => p.Changes)
                .HasForeignKey(e => e.PlanId)
                .HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Date).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        });

        // ── M4 & M5: Ciclos de planificación y Planes Anuales (POA) ──
        modelBuilder.Entity<PlanningCycle>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PlanningCycles");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => e.DeletedAt).HasFilter("[DeletedAt] IS NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.Year })
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL")
                .HasDatabaseName("UX_PlanningCycles_YearPerOrg");

            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.PublishedByName).HasMaxLength(250);
            entity.Property(e => e.CreatedByName).HasMaxLength(250);

            entity.HasOne(e => e.StrategicPlan)
                .WithMany()
                .HasForeignKey(e => e.StrategicPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<PlanningCycleMilestone>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PlanningCycleMilestones");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Kind).HasMaxLength(30);

            entity.HasOne(e => e.Cycle)
                .WithMany(c => c.Milestones)
                .HasForeignKey(e => e.CycleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlanningCycleArea>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PlanningCycleAreas");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.CycleId, e.AreaId })
                .IsUnique()
                .HasDatabaseName("UX_PlanningCycleAreas_CycleArea");

            entity.Property(e => e.AreaName).HasMaxLength(250);

            entity.HasOne(e => e.Cycle)
                .WithMany(c => c.Areas)
                .HasForeignKey(e => e.CycleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnnualPlan>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "AnnualPlans");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.CycleId, e.AreaId })
                .IsUnique()
                .HasDatabaseName("UX_AnnualPlans_CycleArea");

            entity.Property(e => e.AreaName).HasMaxLength(250);
            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.GeneralObservation).HasMaxLength(2000);

            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasOne(e => e.Cycle)
                .WithMany(c => c.Plans)
                .HasForeignKey(e => e.CycleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<PoaProduct>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PoaProducts");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.PlanId, e.Code })
                .IsUnique()
                .HasDatabaseName("UX_PoaProducts_PlanCode");

            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.Origin).HasMaxLength(20);
            entity.Property(e => e.ProductName).HasMaxLength(500);
            entity.Property(e => e.OperationalJustification).HasMaxLength(2000);
            entity.Property(e => e.Observation).HasMaxLength(2000);

            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasOne(e => e.Plan)
                .WithMany(p => p.Products)
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<PoaMeasureUnit>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PoaMeasureUnits");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.TargetType).HasMaxLength(20);
            entity.Property(e => e.VerificationMeans).HasMaxLength(1000);
            entity.Property(e => e.ResponsibleAreaName).HasMaxLength(250);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.MeasureUnits)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PoaTask>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PoaTasks");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(500);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.Tasks)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PoaProductMunicipality>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PoaProductMunicipalities");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.ProductId, e.MunicipioId })
                .IsUnique()
                .HasDatabaseName("UX_PoaProductMunicipalities_ProductMunicipio");

            entity.Property(e => e.MunicipioName).HasMaxLength(250);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.Municipalities)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PoaProductInvolvedArea>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PoaProductInvolvedAreas");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.ProductId, e.AreaId })
                .IsUnique()
                .HasDatabaseName("UX_PoaProductInvolvedAreas_ProductArea");

            entity.Property(e => e.AreaName).HasMaxLength(250);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.InvolvedAreas)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PoaProductCustomValue>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PoaProductCustomValues");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.ProductId, e.TemplateColumnId })
                .IsUnique()
                .HasDatabaseName("UX_PoaProductCustomValues_ProductColumn");

            entity.HasOne(e => e.Product)
                .WithMany(p => p.CustomValues)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TemplateColumn)
                .WithMany()
                .HasForeignKey(e => e.TemplateColumnId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlanningSequence>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PlanningSequences");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.OrganizationId, e.Scope, e.ScopeKey })
                .IsUnique()
                .HasDatabaseName("UX_PlanningSequences_OrgScopeKey");

            entity.Property(e => e.Scope).HasMaxLength(30);
            entity.Property(e => e.ScopeKey).HasMaxLength(80);
            entity.Property(e => e.NextValue).HasDefaultValue(1);
        });

        // ── M7: Requerimientos + Consolidación ──
        modelBuilder.Entity<Requirement>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "Requirements");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.OrganizationId, e.CycleId, e.Status })
                .HasDatabaseName("IX_Requirements_OrgCycleStatus");

            entity.HasIndex(e => new { e.ConsolidatorAreaId, e.Status })
                .HasDatabaseName("IX_Requirements_ConsolidatorStatus");

            entity.HasIndex(e => e.PoaTaskId)
                .HasDatabaseName("IX_Requirements_PoaTaskId");

            entity.HasIndex(e => new { e.MasterProductId, e.Status })
                .HasDatabaseName("IX_Requirements_MasterProductStatus");

            entity.Property(e => e.Code).HasMaxLength(30);
            entity.Property(e => e.OriginAreaName).HasMaxLength(250);
            entity.Property(e => e.ConsolidatorAreaName).HasMaxLength(250);
            entity.Property(e => e.AppliedRule).HasMaxLength(500);
            entity.Property(e => e.MasterProductCode).HasMaxLength(30);
            entity.Property(e => e.MasterProductName).HasMaxLength(300);
            entity.Property(e => e.UnspscCode).HasMaxLength(20);
            entity.Property(e => e.UnspscName).HasMaxLength(500);
            entity.Property(e => e.ExpenseObjectCode).HasMaxLength(20);
            entity.Property(e => e.ExpenseObjectName).HasMaxLength(300);
            entity.Property(e => e.AccountingAccountCode).HasMaxLength(30);
            entity.Property(e => e.AccountingAccountName).HasMaxLength(300);
            entity.Property(e => e.ItemType).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(50);
            entity.Property(e => e.Quarter).HasMaxLength(10);
            entity.Property(e => e.FundingSource).HasMaxLength(100);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.CreatedByName).HasMaxLength(250);

            entity.HasOne(e => e.Cycle)
                .WithMany()
                .HasForeignKey(e => e.CycleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Plan)
                .WithMany()
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PoaProduct)
                .WithMany()
                .HasForeignKey(e => e.PoaProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PoaTask)
                .WithMany()
                .HasForeignKey(e => e.PoaTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.MasterProduct)
                .WithMany()
                .HasForeignKey(e => e.MasterProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.MergedInto)
                .WithMany()
                .HasForeignKey(e => e.MergedIntoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<RequirementComment>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "RequirementComments");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.UserName).HasMaxLength(250);
            entity.Property(e => e.AreaName).HasMaxLength(250);
            entity.Property(e => e.Text).HasMaxLength(2000);

            entity.HasOne(e => e.Requirement)
                .WithMany(r => r.Comments)
                .HasForeignKey(e => e.RequirementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Date).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<PrioritizationDecision>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PrioritizationDecisions");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.Property(e => e.RequirementCode).HasMaxLength(30);
            entity.Property(e => e.Type).HasMaxLength(30);
            entity.Property(e => e.Reason).HasMaxLength(2000);
            entity.Property(e => e.DecidedByName).HasMaxLength(250);
            entity.Property(e => e.Level).HasMaxLength(20);

            entity.HasOne(e => e.Requirement)
                .WithMany(r => r.Decisions)
                .HasForeignKey(e => e.RequirementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Date).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<MasterProduct>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "MasterProducts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.ReferencePrice).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(e => new { e.OrganizationId, e.Code }, "UX_MasterProducts_CodePerOrg")
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.SourceId }, "UX_MasterProducts_SourceIdPerOrg")
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL AND [SourceId] IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.State }, "IX_MasterProducts_OrgState")
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.UnspscCode }, "IX_MasterProducts_OrgUnspsc")
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.ConsolidatorAreaId }, "IX_MasterProducts_OrgConsolidator")
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasIndex(e => e.DeletedAt, "IX_MasterProducts_DeletedAt");

            entity.HasOne(e => e.UnifiedCode)
                .WithMany()
                .HasForeignKey(e => e.UnifiedCodeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ExpenseObject)
                .WithMany()
                .HasForeignKey(e => e.ExpenseObjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AccountingAccount)
                .WithMany()
                .HasForeignKey(e => e.AccountingAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ConsolidatorArea)
                .WithMany()
                .HasForeignKey(e => e.ConsolidatorAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ConsolidatorRule)
                .WithMany()
                .HasForeignKey(e => e.ConsolidatorRuleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.DuplicateOfMasterProduct)
                .WithMany()
                .HasForeignKey(e => e.DuplicateOfMasterProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MasterProductAlias>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "MasterProductAliases");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.MasterProductId, e.Kind, e.Text }, "UX_MasterProductAliases_ProductKindText")
                .IsUnique();

            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedText }, "IX_MasterProductAliases_OrgNormalized");

            entity.HasOne(e => e.MasterProduct)
                .WithMany(p => p.Aliases)
                .HasForeignKey(e => e.MasterProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExpenseObject>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "ExpenseObjects");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(e => new { e.OrganizationId, e.Code }, "UX_ExpenseObjects_CodePerOrg")
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.ParentId }, "IX_ExpenseObjects_OrgParent")
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasOne(e => e.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccountingAccount>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "AccountingAccounts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(e => new { e.OrganizationId, e.Code }, "UX_AccountingAccounts_CodePerOrg")
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.ParentId }, "IX_AccountingAccounts_OrgParent")
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasOne(e => e.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MastersoftAccount>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "MastersoftAccounts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.Name).HasMaxLength(300);
            entity.Property(e => e.ControlAccountCode).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(e => new { e.OrganizationId, e.Code }, "UX_MastersoftAccounts_CodePerOrg")
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasOne(e => e.ControlAccount)
                .WithMany(p => p.SubAccounts)
                .HasForeignKey(e => e.ControlAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MastersoftDigecobMapping>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "MastersoftDigecobMappings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.DigecobAccountCode).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.MastersoftAccountCode).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(e => new { e.OrganizationId, e.DigecobAccountId, e.MastersoftAccountId }, "UX_MastersoftDigecobMappings_UniquePair")
                .IsUnique();

            entity.HasIndex(e => new { e.OrganizationId, e.DigecobAccountCode }, "IX_MastersoftDigecobMappings_OrgDigecobCode");

            entity.HasOne(e => e.DigecobAccount)
                .WithMany(a => a.MastersoftMappings)
                .HasForeignKey(e => e.DigecobAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MastersoftAccount)
                .WithMany(m => m.DigecobMappings)
                .HasForeignKey(e => e.MastersoftAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConsolidatorRoutingRule>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "ConsolidatorRoutingRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(e => new { e.OrganizationId, e.RuleKey, e.Version }, "UX_ConsolidatorRoutingRules_KeyVersion")
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.IsActive, e.Priority }, "IX_ConsolidatorRoutingRules_OrgActivePriority")
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasOne(e => e.ConsolidatorArea)
                .WithMany()
                .HasForeignKey(e => e.ConsolidatorAreaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConsolidatorAreaProfile>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "ConsolidatorAreaProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(e => new { e.OrganizationId, e.AreaCode }, "UX_ConsolidatorAreaProfiles_CodePerOrg")
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasOne(e => e.OrganizationalStructure)
                .WithMany()
                .HasForeignKey(e => e.OrganizationalStructureId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BudgetCeiling>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "BudgetCeilings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InitialAmount).HasPrecision(18, 2);
            entity.Property(e => e.CurrentAmount).HasPrecision(18, 2);
            entity.Property(e => e.ReservedAmount).HasPrecision(18, 2);
            entity.Property(e => e.PlannedAmount).HasPrecision(18, 2);
            entity.Property(e => e.CommittedAmount).HasPrecision(18, 2);
            entity.Property(e => e.ExecutedAmount).HasPrecision(18, 2);
            entity.Property(e => e.AvailableAmount).HasPrecision(18, 2);
            entity.Property(e => e.ThresholdPercentage).HasPrecision(5, 2).HasDefaultValue(10.00m);

            entity.HasIndex(e => new { e.OrganizationId, e.FiscalYear, e.CycleId, e.ExpenseObjectId, e.FundingSource }, "UX_BudgetCeilings_OrgYearCycleAuxFunding")
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");

            entity.HasOne(e => e.ExpenseObject)
                .WithMany()
                .HasForeignKey(e => e.ExpenseObjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Cycle)
                .WithMany(c => c.BudgetCeilings)
                .HasForeignKey(e => e.CycleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BudgetAdjustment>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "BudgetAdjustments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.BudgetCeiling)
                .WithMany(c => c.Adjustments)
                .HasForeignKey(e => e.BudgetCeilingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlanningCycleAmendmentLog>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "PlanningCycleAmendmentLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();

            entity.HasOne(e => e.Cycle)
                .WithMany(c => c.AmendmentLogs)
                .HasForeignKey(e => e.CycleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FundingSource>(entity =>
        {
            entity.ToTable(schema: "Planning", name: "FundingSources");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.OrganizationId, e.Code }, "UX_FundingSources_OrgCode")
                .IsUnique()
                .HasFilter("[DeletedAt] IS NULL");
        });
    }
}

