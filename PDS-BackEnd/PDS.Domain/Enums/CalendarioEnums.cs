namespace PDS.Domain.Enums;

// SPEC-LOC-CAL-01 §9.5. Todos se persisten como texto (HasConversion<string>) y se serializan con JsonStringEnumConverter.

/// <summary>Tipo de excepción del calendario de una localidad.</summary>
public enum TipoExcepcionCalendario
{
    Cierre,
    JornadaEspecial,
    DiaLaborableAdicional
}

/// <summary>Categoría de una excepción. <see cref="Suspension"/> y <see cref="CierreDefinitivo"/> son solo de sistema.</summary>
public enum CategoriaExcepcionCalendario
{
    DecisionAdministrativa,
    FeriadoLocal,
    DueloOficial,
    Emergencia,
    EventoInstitucional,
    Otro,
    Suspension,
    CierreDefinitivo
}

/// <summary>Origen de una excepción de usuario.</summary>
public enum OrigenExcepcionCalendario
{
    PaisAnfitrion,
    PaisPropio,
    Institucional
}

/// <summary>Alcance de un feriado: día completo o jornada reducida.</summary>
public enum AlcanceFeriado
{
    DiaCompleto,
    JornadaReducida
}

/// <summary>Ámbito de un calendario de feriados.</summary>
public enum AmbitoCalendarioFeriados
{
    Nacional,
    Subnacional,
    Local,
    Institucional,
    OrganismoInternacional
}

/// <summary>Qué determinó el estado de un día en el calendario efectivo.</summary>
public enum FuenteDiaCalendario
{
    Jornada,
    Feriado,
    Excepcion,
    CicloVida
}

/// <summary>Motivo de una fila del feed de cambios del calendario (RF-LO-43).</summary>
public enum MotivoCambioCalendario
{
    ZonaHoraria,
    Jornada,
    Suscripcion,
    Excepcion,
    Feriado,
    CicloVida,
    Migracion
}

/// <summary>Canal por el que se hizo un cambio de calendario (RF-LO-44).</summary>
public enum CanalCambio
{
    PdsUi,
    ApiExterna,
    Sistema,
    Migracion
}
