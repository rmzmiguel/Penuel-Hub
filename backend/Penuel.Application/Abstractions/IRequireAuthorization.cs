namespace Penuel.Application.Abstractions;

/// <summary>
/// Mecanismo general de autorización de un caso de uso: se permite si quien llama tiene
/// AL MENOS UNO de los roles aceptados, O AL MENOS UNO de los cargos aceptados activo.
/// </summary>
/// <remarks>
/// Existe porque <see cref="IRequirePastor"/> se quedó corto: la Sección 8.3 de la rama de
/// Servicios exige "Tesorero General o Pastor", y un <c>Position</c> NO viaja en el JWT —
/// verificarlo obliga a consultar <c>person_positions</c> contra la base.
///
/// Es deliberadamente un MECANISMO, sin nombres concretos: los nombres los pone cada rama en
/// sus propios marcadores, para que este archivo del Core no tenga que conocerlos.
///
/// Los cargos solo se consultan si la comprobación de roles no bastó, así que el caso normal
/// (el Pastor) no paga ninguna consulta extra.
/// </remarks>
public interface IRequireAuthorization
{
    /// <summary>Roles de sistema que bastan por sí solos. Se leen de los claims del token.</summary>
    IReadOnlyCollection<string> AcceptedRoles { get; }

    /// <summary>
    /// Cargos que bastan por sí solos. Se resuelven contra la base, porque no están en el token
    /// — lo que además hace que revocar un cargo surta efecto de inmediato, igual que un rol.
    /// </summary>
    IReadOnlyCollection<string> AcceptedPositions { get; }
}
