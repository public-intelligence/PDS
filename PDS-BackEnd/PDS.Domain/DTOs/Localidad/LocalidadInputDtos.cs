namespace PDS.Domain.DTOs.Localidad;

// ── Sub-elementos de entrada (anidados en crear/editar) ──
public record LocalidadServicioInputDto
{
    public Guid ServicioId { get; init; }
    public bool Habilitado { get; init; } = true;
    public DateOnly? FechaHabilitacion { get; init; }
}

public record LocalidadResponsableInputDto
{
    public Guid ResponsableId { get; init; }
    public string? Rol { get; init; }
    public bool Principal { get; init; }
    public DateOnly? FechaAsignacion { get; init; }

    /// <summary>RF-LO-38 (solo administradores de calendario). Null = no cambiar.</summary>
    public bool? GestionaCalendario { get; init; }
}

public record LocalidadDocumentoInputDto
{
    public required string TipoDocumento { get; init; }
    public string? Descripcion { get; init; }
    public required string Url { get; init; }
}

public record LocalidadUnidadPresenciaInputDto
{
    public Guid UnidadOrganizativaId { get; init; }
    public string? Notas { get; init; }
}

public record LocalidadHorarioExcepcionInputDto
{
    public required string Descripcion { get; init; }
    public DateOnly FechaInicio { get; init; }
    public DateOnly? FechaFin { get; init; }
    public string? Horario { get; init; }
}

public record LocalidadCoberturaInputDto
{
    public Guid? RegionId { get; init; }
    public Guid? ProvinciaId { get; init; }
    public Guid? MunicipioId { get; init; }
    public Guid? DistritoMunicipalId { get; init; }
}

public record LocalidadInstrumentoLegalInputDto
{
    public Guid InstrumentoLegalId { get; init; }
    public string? Proposito { get; init; }
}

/// <summary>Crea una localidad (RF-LO-01) con todos sus campos y, opcionalmente, sus colecciones hijas.</summary>
public record CreateLocalidadDto
{
    // Identificación
    public required string Codigo { get; init; }
    public required string NombreOficial { get; init; }
    public string? NombreCorto { get; init; }
    public string? Sigla { get; init; }

    // Clasificación
    public Guid TipoLocalidadId { get; init; }
    public Guid NaturalezaAdministrativaId { get; init; }
    public Guid? EstadoLocalidadId { get; init; }

    // Vínculos con la estructura organizativa (RF-LO-02)
    public Guid? UnidadResponsableId { get; init; }
    public Guid? UnidadVinculadaId { get; init; }

    // Jerarquía
    public Guid? LocalidadPadreId { get; init; }

    // Ubicación territorial
    public Guid PaisId { get; init; }
    public Guid? RegionId { get; init; }
    public Guid? ProvinciaId { get; init; }
    public Guid? MunicipioId { get; init; }
    public Guid? DistritoMunicipalId { get; init; }
    public string? Ciudad { get; init; }
    public string? SubdivisionAdministrativa { get; init; }
    public string? SectorBarrioParaje { get; init; }
    public required string DireccionCompleta { get; init; }
    public string? EdificioPisoLocalReferenciaInterna { get; init; }
    public string? CodigoPostal { get; init; }

    // Coordenadas
    public double? Latitud { get; init; }
    public double? Longitud { get; init; }

    // Acceso / horario / notas
    public string? NotasAcceso { get; init; }
    public string? HorarioRegular { get; init; }
    public string? Observaciones { get; init; }
    public DateOnly? FechaApertura { get; init; }

    /// <summary>Zona horaria IANA. Omitida = America/Santo_Domingo. Solo administradores pueden indicarla.</summary>
    public string? ZonaHoraria { get; init; }

    // Validación de duplicados (RF-LO-13): permite continuar con justificación.
    public bool Override { get; init; }
    public string? JustificacionDuplicado { get; init; }

    // Colecciones hijas (opcionales)
    public List<LocalidadServicioInputDto>? Servicios { get; init; }
    public List<LocalidadResponsableInputDto>? Responsables { get; init; }

    /// <summary>
    /// RF-LO-45: ids de <c>FuncionLocalidad</c>. Al crear, omitido o vacío = sin funciones. Al actualizar, omitido (null) = no tocar;
    /// lista vacía = quitar todas. <c>SECCION_CONSULAR</c> exige <c>EMBAJADA</c> en la misma localidad (400, clave <c>funciones</c>).
    /// </summary>
    public List<Guid>? FuncionIds { get; init; }
    public List<LocalidadDocumentoInputDto>? Documentos { get; init; }
    public List<LocalidadUnidadPresenciaInputDto>? UnidadesPresencia { get; init; }
    public List<LocalidadHorarioExcepcionInputDto>? HorariosExcepcion { get; init; }
    public List<LocalidadCoberturaInputDto>? Coberturas { get; init; }
    public List<LocalidadInstrumentoLegalInputDto>? InstrumentosLegales { get; init; }
}

/// <summary>
/// Modifica una localidad (RF-LO-06). Requiere <see cref="Motivo"/> y, opcionalmente, el instrumento
/// legal de respaldo. Las colecciones hijas, si se envían (no nulas), reemplazan por completo a las
/// existentes; si se omiten (nulas) se dejan intactas.
/// </summary>
public record UpdateLocalidadDto
{
    public required string Codigo { get; init; }
    public required string NombreOficial { get; init; }
    public string? NombreCorto { get; init; }
    public string? Sigla { get; init; }

    public Guid TipoLocalidadId { get; init; }
    public Guid NaturalezaAdministrativaId { get; init; }

    public Guid? UnidadResponsableId { get; init; }
    public Guid? UnidadVinculadaId { get; init; }
    public Guid? LocalidadPadreId { get; init; }

    public Guid PaisId { get; init; }
    public Guid? RegionId { get; init; }
    public Guid? ProvinciaId { get; init; }
    public Guid? MunicipioId { get; init; }
    public Guid? DistritoMunicipalId { get; init; }
    public string? Ciudad { get; init; }
    public string? SubdivisionAdministrativa { get; init; }
    public string? SectorBarrioParaje { get; init; }
    public required string DireccionCompleta { get; init; }
    public string? EdificioPisoLocalReferenciaInterna { get; init; }
    public string? CodigoPostal { get; init; }

    public double? Latitud { get; init; }
    public double? Longitud { get; init; }

    public string? NotasAcceso { get; init; }
    public string? HorarioRegular { get; init; }
    public string? Observaciones { get; init; }
    public DateOnly? FechaApertura { get; init; }

    /// <summary>Zona horaria IANA. Omitida = sin cambios. Solo administradores pueden modificarla.</summary>
    public string? ZonaHoraria { get; init; }

    // Trazabilidad del cambio (RF-LO-06)
    public required string Motivo { get; init; }
    public Guid? InstrumentoLegalId { get; init; }

    // Duplicados (RF-LO-13)
    public bool Override { get; init; }
    public string? JustificacionDuplicado { get; init; }

    // Colecciones hijas (null = no tocar; lista = reemplazar)
    public List<LocalidadServicioInputDto>? Servicios { get; init; }
    public List<LocalidadResponsableInputDto>? Responsables { get; init; }

    /// <summary>
    /// RF-LO-45: ids de <c>FuncionLocalidad</c>. Al crear, omitido o vacío = sin funciones. Al actualizar, omitido (null) = no tocar;
    /// lista vacía = quitar todas. <c>SECCION_CONSULAR</c> exige <c>EMBAJADA</c> en la misma localidad (400, clave <c>funciones</c>).
    /// </summary>
    public List<Guid>? FuncionIds { get; init; }
    public List<LocalidadDocumentoInputDto>? Documentos { get; init; }
    public List<LocalidadUnidadPresenciaInputDto>? UnidadesPresencia { get; init; }
    public List<LocalidadHorarioExcepcionInputDto>? HorariosExcepcion { get; init; }
    public List<LocalidadCoberturaInputDto>? Coberturas { get; init; }
    public List<LocalidadInstrumentoLegalInputDto>? InstrumentosLegales { get; init; }
}
