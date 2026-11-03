using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PDS.Application.Data;

/// <summary>
/// Fábrica de <see cref="PDSDbContext"/> para las herramientas de EF Core en tiempo de diseño
/// (<c>dotnet ef migrations add</c>, <c>migrations bundle</c>, <c>database update</c>).
/// </summary>
/// <remarks>
/// <para>
/// Mismo patrón que <c>NormDbContextDesignTimeFactory</c> en NORM, y por la misma colisión: sin
/// fábrica, EF construye el host de <c>PDS</c> para conseguir el contexto, y ese arranque es
/// fail-closed (<c>ConnectionStrings:DefaultConnection</c>, <c>Jwt:Secret</c>, <c>GobidAuth</c>).
/// En un agente de compilación no hay secretos, así que el bundle moría al generarse y al
/// ejecutarse. Verificado en las corridas 1962, 1965 y 1968 de PDS_BackEnd_Production.
/// </para>
/// <para>
/// Con la fábrica, las herramientas ya no levantan la aplicación: el arranque sigue estricto y el
/// bundle funciona sin secretos. La cadena real la recibe el bundle al ejecutarse, con
/// <c>--connection</c>.
/// </para>
/// <para>
/// <b>Ojo con la precedencia:</b> cuando esta clase existe, EF la prefiere por encima del host.
/// Para <c>dotnet ef database update</c> en local hay que definir la variable de entorno
/// <c>ConnectionStrings__DefaultConnection</c>; sin ella la fábrica usa un relleno que no apunta
/// a ninguna base real, que es lo correcto.
/// </para>
/// </remarks>
public sealed class PDSDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PDSDbContext>
{
    /// <summary>
    /// Cadena de relleno para las operaciones que <b>no</b> abren conexión: <c>migrations add</c>
    /// y <c>migrations bundle</c> solo necesitan que el proveedor esté configurado. Cualquier
    /// operación que sí necesite la base va a fallar contra este valor, y eso es lo correcto:
    /// mejor que apuntar a algo real por accidente.
    /// </summary>
    private const string SinConfigurar =
        "Server=(localdb)\\MSSQLLocalDB;Database=PDS-DesignTime;Trusted_Connection=True;TrustServerCertificate=True";

    public PDSDbContext CreateDbContext(string[] args)
    {
        // Solo variables de entorno, sin ConfigurationBuilder: PDS.Application no referencia los
        // paquetes de configuración y para las herramientas de diseño alcanza con esto.
        var cadena = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(cadena))
        {
            cadena = SinConfigurar;
        }

        var opciones = new DbContextOptionsBuilder<PDSDbContext>()
            .UseSqlServer(cadena)
            .Options;

        return new PDSDbContext(opciones);
    }
}
