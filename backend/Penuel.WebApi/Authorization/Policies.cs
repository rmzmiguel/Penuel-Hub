using Penuel.Domain.Constants;

namespace Penuel.WebApi.Authorization;

/// <summary>
/// Políticas nombradas por INTENCIÓN, no por nombre de rol suelto (Sección 8.2).
/// El día que "quién puede otorgar roles" deje de ser únicamente el Pastor, cambia
/// la definición de la política en un solo lugar y no veinte atributos.
/// </summary>
public static class Policies
{
    /// <summary>
    /// Operaciones de administración. Hoy las cubre el Pastor; los superusuarios
    /// (<see cref="RoleNames.Superusers"/>) las superan todas, y eso se compone UNA vez en el
    /// registro de políticas de <c>Program.cs</c>, no política por política.
    /// </summary>
    public const string RequirePastor = "RequirePastorPolicy";

    /// <summary>
    /// Operar la captura de Escuela Dominical. Se puede expresar como política de controlador
    /// porque es un ROL y los roles viajan en el token.
    /// </summary>
    public const string RequireSundaySchoolRecorder = "RequireSundaySchoolRecorderPolicy";

    // NOTA: el acceso de tesorería (Pastor o cargo Tesorero General) NO tiene política aquí,
    // y es a propósito: un Position no viaja en el JWT, así que ninguna política de ASP.NET
    // puede evaluarlo sin consultar la base. Esos endpoints llevan [Authorize] a secas y la
    // decisión real la toma AuthorizationBehavior en Penuel.Application, que es donde la
    // Sección 5.4 del Core dice que vive la autorización.
}
