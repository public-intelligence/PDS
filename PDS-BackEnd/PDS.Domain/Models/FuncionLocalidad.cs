using PDS.Domain.Enums;

namespace PDS.Domain.Models;

/// <summary>
/// Catálogo global de funciones de localidad del servicio exterior: embajada, misión permanente,
/// consulado general, sección consular (RF-LO-45). Se asigna a las localidades mediante
/// <see cref="LocalidadFuncion"/>. Dato de referencia compartido (no pertenece a una organización).
/// </summary>
public class FuncionLocalidad : ISoftDeletable
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public CategoriaFuncionLocalidad Categoria { get; set; }
    public string? Descripcion { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>Relación N:M entre una localidad y sus funciones (RF-LO-45). Pareja única, sin borrado lógico.</summary>
public class LocalidadFuncion
{
    public Guid Id { get; set; }
    public Guid LocalidadId { get; set; }
    public Guid FuncionLocalidadId { get; set; }

    public Localidad? Localidad { get; set; }
    public FuncionLocalidad? FuncionLocalidad { get; set; }
}
