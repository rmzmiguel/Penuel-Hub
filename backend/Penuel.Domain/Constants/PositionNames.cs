namespace Penuel.Domain.Constants;

/// <summary>
/// Nombres de los cargos eclesiásticos sembrados. Es el análogo de <see cref="RoleNames"/>
/// para el eje de <c>Position</c>, y existe por la misma razón (regla 7.7 del Core): en cuanto
/// el código necesita referirse a un cargo por su nombre, ese nombre deja de poder andar suelto
/// entre comillas.
/// </summary>
/// <remarks>
/// Deben coincidir EXACTAMENTE con los valores sembrados en la migración inicial: la búsqueda
/// se hace por nombre contra la tabla <c>positions</c>.
/// Recuerda que un <c>Position</c> NO es un permiso de sistema (Sección 3.4 del Core):
/// que aquí se use para autorizar es una excepción deliberada y acotada, descrita en la
/// Sección 8.3 de la rama de Servicios.
/// </remarks>
public static class PositionNames
{
    public const string Pastor = "Pastor";
    public const string Diacono = "Diácono";
    public const string SecretarioGeneral = "Secretario General";
    public const string TesoreroGeneral = "Tesorero General";
}
