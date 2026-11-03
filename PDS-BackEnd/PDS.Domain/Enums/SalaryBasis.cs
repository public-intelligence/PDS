namespace PDS.Domain.Enums;

/// <summary>
/// Base de cálculo del costo estimado a partir del rango salarial (mínimo, punto medio o máximo).
/// El costo es siempre una estimación derivada de plazas autorizadas, no nómina real.
/// </summary>
public enum SalaryBasis
{
    Min,
    Midpoint,
    Max
}
