namespace PDS.Service.Authorization;

/// <summary>
/// Permission strings del módulo de Planificación. Viajan en el claim "permissions" del JWT
/// de GOBID (JSON string[]) y se aplican con checks explícitos en los servicios
/// (IPlanningAccessGuard) — no con policies de ASP.NET, siguiendo el estilo del backend.
/// </summary>
public static class PlanningPermissions
{
    /// <summary>Acceso total al módulo de Planificación.</summary>
    public const string Admin = "planning:admin";

    /// <summary>Crear/publicar/cerrar ciclos.</summary>
    public const string CyclesManage = "planning:cycles:manage";

    /// <summary>Plantillas del plan anual, matriz de códigos y categorías de apoyo.</summary>
    public const string ConfigManage = "planning:config:manage";

    /// <summary>Iniciar enmiendas y editar el borrador del PEI (Planificación).</summary>
    public const string PeiEdit = "planning:pei:edit";

    /// <summary>Aprobar enmiendas del PEI (MAE).</summary>
    public const string PeiApprove = "planning:pei:approve";

    /// <summary>Editar/someter el POA (condicionado además al área propia).</summary>
    public const string PoaEdit = "planning:poa:edit";

    /// <summary>Devolver/aprobar planes, consolidación y fase de ajustes (Planificación).</summary>
    public const string PoaReview = "planning:poa:review";

    /// <summary>Crear/editar/eliminar requerimientos (condicionado al área origen).</summary>
    public const string RequirementsEdit = "planning:requirements:edit";

    /// <summary>Estimar precio, decidir, reducir y fusionar (condicionado a pertenecer a la consolidadora).</summary>
    public const string RequirementsConsolidate = "planning:requirements:consolidate";

    /// <summary>Lectura de la matriz de códigos por otros sistemas (requiere además access:M2M).</summary>
    public const string CatalogRead = "planning:catalog:read";

    /// <summary>Administración de la maestra institucional de artículos y catálogos regulatorios.</summary>
    public const string CatalogManage = "planning:catalog:manage";

    /// <summary>
    /// Modificar el organigrama (crear, editar, eliminar o importar unidades). Todo el
    /// alcance por área del ecosistema se resuelve contra esta estructura, así que cambiarla
    /// es una acción de administración, no de uso diario.
    /// </summary>
    public const string StructureManage = "planning:structure:manage";

    /// <summary>Todos los permisos del módulo (usado por PlanningDevOverrides).</summary>
    public static readonly IReadOnlyCollection<string> All =
    [
        Admin,
        CyclesManage,
        ConfigManage,
        PeiEdit,
        PeiApprove,
        PoaEdit,
        PoaReview,
        RequirementsEdit,
        RequirementsConsolidate,
        CatalogRead,
        CatalogManage,
        StructureManage
    ];
}
