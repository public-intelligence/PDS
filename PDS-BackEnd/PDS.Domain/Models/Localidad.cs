namespace PDS.Domain.Models;

/// <summary>
/// Localidad institucional: sitio físico, territorial u operativo de la institución
/// (sede, oficina regional/provincial/municipal, ventanilla, almacén, archivo, punto temporal, etc.).
/// Una localidad NO es una unidad organizativa (RF-LO-02); puede vincularse opcionalmente a una
/// sin duplicar datos. No se elimina físicamente: las bajas son lógicas (estado + DeletedAt).
/// </summary>
public class Localidad : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    // ── Identificación (RF-LO-01) ──
    public required string Codigo { get; set; }
    public required string NombreOficial { get; set; }
    public string? NombreCorto { get; set; }
    public string? Sigla { get; set; }

    // ── Clasificación ──
    public Guid TipoLocalidadId { get; set; }
    public Guid NaturalezaAdministrativaId { get; set; }
    public Guid EstadoLocalidadId { get; set; }

    // ── Vínculos con la estructura organizativa (RF-LO-02; opcionales, nunca autocreados) ──
    public Guid? UnidadResponsableId { get; set; }
    public Guid? UnidadVinculadaId { get; set; }

    // ── Jerarquía territorial de localidades (RF-LO-03) ──
    public Guid? LocalidadPadreId { get; set; }

    // ── Ubicación territorial ──
    public Guid PaisId { get; set; }
    public Guid? RegionId { get; set; }
    public Guid? ProvinciaId { get; set; }
    public Guid? MunicipioId { get; set; }
    public Guid? DistritoMunicipalId { get; set; }

    /// <summary>Ciudad. Obligatoria en direcciones en el exterior; opcional para República Dominicana.</summary>
    public string? Ciudad { get; set; }

    /// <summary>Estado, provincia o departamento del país en el exterior (texto libre).</summary>
    public string? SubdivisionAdministrativa { get; set; }
    public string? SectorBarrioParaje { get; set; }
    public required string DireccionCompleta { get; set; }
    public string? EdificioPisoLocalReferenciaInterna { get; set; }
    public string? CodigoPostal { get; set; }

    /// <summary>Zona horaria IANA de la localidad (RF-LO-20). Solo la modifica un administrador.</summary>
    public string ZonaHoraria { get; set; } = "America/Santo_Domingo";

    /// <summary>
    /// RF-LO-43: versión del calendario efectivo. Se incrementa (junto con una fila de <see cref="CalendarioCambio"/>,
    /// en la misma transacción) cada vez que el calendario efectivo puede cambiar.
    /// </summary>
    public long CalendarioVersion { get; set; } = 1;

    /// <summary>RF-LO-36: localidad cuyo calendario es el de respaldo y la plantilla. A lo sumo una por organización.</summary>
    public bool EsLocalidadCalendarioPredeterminada { get; set; }

    // ── Coordenadas ──
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }

    // ── Acceso / horario / notas ──
    public string? NotasAcceso { get; set; }
    public string? HorarioRegular { get; set; }
    public string? Observaciones { get; set; }

    // ── Ciclo de vida ──
    public DateOnly? FechaApertura { get; set; }

    // Suspensión temporal (RF-LO-09)
    public string? SuspensionMotivo { get; set; }
    public DateOnly? SuspensionFechaInicio { get; set; }
    public DateOnly? SuspensionFechaFin { get; set; }
    public Guid? SuspensionInstrumentoLegalId { get; set; }
    public Guid? LocalidadAlternaId { get; set; }

    // Cierre / desactivación (RF-LO-10)
    public DateOnly? CierreFecha { get; set; }
    public string? CierreMotivo { get; set; }
    public Guid? CierreInstrumentoLegalId { get; set; }
    public Guid? LocalidadSucesoraId { get; set; }
    public string? ServiciosAfectados { get; set; }

    // ── Validación de duplicados (RF-LO-13): justificación persistida del override ──
    public string? JustificacionDuplicado { get; set; }

    // ── Auditoría base ──
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // ── Navegaciones ──
    public TipoLocalidad? Tipo { get; set; }
    public NaturalezaAdministrativa? Naturaleza { get; set; }
    public EstadoLocalidad? Estado { get; set; }

    public OrganizationalStructure? UnidadResponsable { get; set; }
    public OrganizationalStructure? UnidadVinculada { get; set; }

    public Pais? Pais { get; set; }
    public Region? Region { get; set; }
    public Provincia? Provincia { get; set; }
    public Municipio? Municipio { get; set; }
    public DistritoMunicipal? DistritoMunicipal { get; set; }

    public Localidad? Padre { get; set; }
    public Localidad? Alterna { get; set; }
    public Localidad? Sucesora { get; set; }
    public ICollection<Localidad> Hijos { get; } = [];

    public ICollection<LocalidadHorarioExcepcion> HorariosExcepcion { get; } = [];
    public ICollection<LocalidadUnidadPresencia> UnidadesPresencia { get; } = [];
    public ICollection<LocalidadCoberturaTerritorial> Coberturas { get; } = [];
    public ICollection<LocalidadInstrumentoLegal> InstrumentosLegales { get; } = [];
    public ICollection<LocalidadDocumento> Documentos { get; } = [];
    public ICollection<LocalidadServicio> Servicios { get; } = [];
    public ICollection<LocalidadResponsable> Responsables { get; } = [];

    /// <summary>RF-LO-45: funciones de servicio exterior (embajada, misión permanente, consulado general, sección consular).</summary>
    public ICollection<LocalidadFuncion> Funciones { get; } = [];
    public ICollection<LocalidadAuditoria> Auditorias { get; } = [];
}
