namespace PDS.Domain.Models;

/// <summary>
/// Documento de respaldo de una localidad (RF-LO-12). Por ahora solo se almacena la URL más el tipo y
/// la descripción. Las columnas <see cref="StoragePath"/>/<see cref="ContentType"/> quedan reservadas
/// (nulas) para incorporar carga de archivos en el futuro sin romper el esquema.
/// </summary>
public class LocalidadDocumento : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid LocalidadId { get; set; }
    public required string TipoDocumento { get; set; }
    public string? Descripcion { get; set; }
    public required string Url { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string? RegistradoPor { get; set; }

    // Reservado para almacenamiento de archivos futuro.
    public string? StoragePath { get; set; }
    public string? ContentType { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Localidad? Localidad { get; set; }
}
