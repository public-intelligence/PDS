namespace PDS.Domain.Enums;

/// <summary>
/// Estados del plan anual (M5). Se persisten EN ESPAÑOL: es el vocabulario del contrato
/// funcional aprobado, lo que escribe PublishAsync al generar los planes y lo que muestra
/// la UI sin adapters. Constantes (no enum) porque los literales llevan espacios/acentos.
/// </summary>
public static class AnnualPlanStatuses
{
    public const string SinIniciar = "Sin iniciar";
    public const string Borrador = "Borrador";
    public const string ListoParaRevision = "Listo para revisión";
    public const string Observado = "Observado";
    public const string EnAjustes = "En ajustes";
    public const string Aprobado = "Aprobado";
    public const string EnEjecucion = "En ejecución";
    public const string Cerrado = "Cerrado";

    /// <summary>Estados en los que el área dueña puede editar su plan.</summary>
    public static readonly IReadOnlyCollection<string> Editables =
        [SinIniciar, Borrador, Observado];
}
