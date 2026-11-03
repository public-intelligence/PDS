using System.Text.Json.Serialization;
using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.Calendario;

/// <summary>Intervalo laboral de la API: horas locales <c>HH:mm</c> (<c>24:00</c> solo como fin). SPEC-LOC-CAL-01 §9.4.</summary>
public sealed record IntervaloDto(string Inicio, string Fin);

/// <summary>Filtros del listado de calendarios de feriados (RF-LO-27).</summary>
public class CalendarioFeriadosFilterDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? PaisId { get; set; }

    /// <summary>Busca por código o nombre.</summary>
    public string? Q { get; set; }
}

/// <summary>Elemento del listado (UI-09). Contrato acordado con el frontend.</summary>
public class CalendarioFeriadosListItemDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public Guid? PaisId { get; set; }
    public string? PaisNombre { get; set; }
    public AmbitoCalendarioFeriados Ambito { get; set; }
    public Guid? LocalidadGestoraId { get; set; }
    public string? LocalidadGestoraNombre { get; set; }
    public bool Activo { get; set; }
    public int LocalidadesSuscritas { get; set; }
    public int FeriadosAnioActual { get; set; }
    public int FeriadosAnioSiguiente { get; set; }

    /// <summary>True si quien consulta puede mantener las entradas del calendario (administrador o gestor de la localidad gestora).</summary>
    public bool PuedeGestionar { get; set; }
}

/// <summary>Localidad suscrita a un calendario de feriados (pestaña «Localidades suscritas», UI-10).</summary>
public class SuscriptorCalendarioDto
{
    public Guid LocalidadId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Pais { get; set; }
    public DateOnly? VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
}

/// <summary>Detalle de un calendario de feriados: el elemento del listado más la ventana retroactiva y los suscriptores.</summary>
public class CalendarioFeriadosDetailDto : CalendarioFeriadosListItemDto
{
    /// <summary>RF-LO-33: días hacia atrás que un gestor no administrador puede modificar (<c>Calendario:VentanaRetroactivaDias</c>).</summary>
    public int VentanaRetroactivaDias { get; set; }

    public IReadOnlyList<SuscriptorCalendarioDto> Suscriptores { get; set; } = [];

    /// <summary>ETag fuerte derivado de <c>RowVersion</c> (RF-LO-42). No se serializa: va en el encabezado.</summary>
    [JsonIgnore]
    public string? ETag { get; set; }
}

/// <summary>Cuerpo de alta y edición de un calendario de feriados (§11.3.4 más <c>descripcion</c> y <c>activo</c> de RF-LO-27).</summary>
public class UpsertCalendarioFeriadosDto
{
    /// <summary>Único por organización. No se puede cambiar después de crear el calendario.</summary>
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public Guid? PaisId { get; set; }
    public AmbitoCalendarioFeriados? Ambito { get; set; }
    public Guid? LocalidadGestoraId { get; set; }

    /// <summary>Nulo = activo al crear; al editar, nulo conserva el valor actual.</summary>
    public bool? Activo { get; set; }
}

/// <summary>Entrada (feriado) de un calendario (RF-LO-28).</summary>
public class FeriadoDiaDto
{
    public Guid Id { get; set; }
    public DateOnly Fecha { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public AlcanceFeriado Alcance { get; set; }

    /// <summary>Horario que SE TRABAJA en una jornada reducida; nulo en <see cref="AlcanceFeriado.DiaCompleto"/>.</summary>
    public IReadOnlyList<IntervaloDto>? Intervalos { get; set; }
    public string? ReferenciaNormativa { get; set; }
    public string? Notas { get; set; }

    [JsonIgnore]
    public string? ETag { get; set; }
}

/// <summary>Cuerpo de alta y edición de un feriado.</summary>
public class UpsertFeriadoDiaDto
{
    public DateOnly? Fecha { get; set; }
    public string? Nombre { get; set; }
    public AlcanceFeriado? Alcance { get; set; }
    public List<IntervaloDto>? Intervalos { get; set; }
    public string? ReferenciaNormativa { get; set; }
    public string? Notas { get; set; }

    /// <summary>RF-LO-33: obligatorio cuando un administrador cambia fechas pasadas.</summary>
    public string? Motivo { get; set; }
}

/// <summary>Modo de la carga masiva de feriados (RF-LO-29).</summary>
public enum ModoImportacionFeriados
{
    /// <summary>Crea las fechas nuevas, actualiza las que cambian y omite las idénticas. No elimina nada.</summary>
    Agregar,

    /// <summary>Además de lo anterior, elimina los feriados de los años presentes en la carga que no vienen en ella.</summary>
    ReemplazarAnio,
}

/// <summary>Error de una línea (base 1) de la carga masiva.</summary>
public class ErrorImportacionFeriadoDto
{
    public int Linea { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>Resultado de la carga masiva (RF-LO-29). Con errores no se guarda nada y los contadores quedan en cero.</summary>
public class ImportacionFeriadosResultadoDto
{
    public int Creados { get; set; }
    public int Actualizados { get; set; }
    public int Omitidos { get; set; }

    /// <summary>Feriados eliminados por <see cref="ModoImportacionFeriados.ReemplazarAnio"/>.</summary>
    public int Eliminados { get; set; }
    public IReadOnlyList<ErrorImportacionFeriadoDto> Errores { get; set; } = [];
}
