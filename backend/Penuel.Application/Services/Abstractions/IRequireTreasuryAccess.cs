using Penuel.Application.Abstractions;
using Penuel.Domain.Constants;

namespace Penuel.Application.Services.Abstractions;

/// <summary>
/// Captura y consulta de lo que toca dinero en los cultos: el Pastor, o quien ostente
/// activamente el cargo de Tesorero General (Sección 8.3).
/// </summary>
/// <remarks>
/// Nótese que el Tesorero entra por su CARGO, no por un rol de sistema: no existe (ni hace
/// falta) un <c>Role</c> "Treasurer". Es el único punto de todo el sistema donde un
/// <c>Position</c> concede acceso, y es una excepción consciente y acotada a la regla 7.10
/// del Core.
/// </remarks>
public interface IRequireTreasuryAccess : IRequireAuthorization
{
    IReadOnlyCollection<string> IRequireAuthorization.AcceptedRoles => [RoleNames.Pastor];

    IReadOnlyCollection<string> IRequireAuthorization.AcceptedPositions =>
        [PositionNames.TesoreroGeneral];
}
