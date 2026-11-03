using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

namespace PDS;

/// <summary>
/// Traduce los nombres de los secretos del vault a las claves de configuracion que la API lee.
/// </summary>
/// <remarks>
/// Este sistema comparte el vault de QA con los demas que no tienen uno propio, asi que sus
/// secretos van prefijados con el nombre del sistema. El Load() de abajo solo acepta los de
/// este prefijo: la aplicacion no carga ni ve los secretos de los otros sistemas, aunque el
/// vault los contenga.
///
/// La traduccion hace falta porque las dos convenciones no coinciden. En el vault los nombres
/// van en kebab-case, como fija la estrategia de secretos de la VUC: ahi el nombre es libre
/// porque los App Service los consumen por referencias @Microsoft.KeyVault. El proveedor de
/// configuracion de .NET, en cambio, deriva la clave del nombre: convierte `--` en `:` y deja
/// el resto igual. Sin traducir, el vault se lee sin error y la API igual no encuentra sus
/// secretos.
///
/// El mapa es explicito porque no hay regla mecanica que lo derive, y porque asi un secreto
/// nuevo con un nombre que la API no conoce queda fuera en lugar de colarse con una clave que
/// nadie lee.
/// </remarks>
public sealed class SecretosDelVaultLocal : KeyVaultSecretManager
{
    private static readonly Dictionary<string, string> Equivalencias =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["pds-sql-defaultconnection"] = "ConnectionStrings:DefaultConnection",
            ["pds-jwt-secret"] = "Jwt:Secret",
            ["pds-gobidm2m-clientsecret"] = "GobidM2M:ClientSecret",
        };

    public override bool Load(SecretProperties properties) =>
        Equivalencias.ContainsKey(properties.Name);

    public override string GetKey(KeyVaultSecret secret) =>
        Equivalencias.TryGetValue(secret.Name, out var clave) ? clave : base.GetKey(secret);
}
