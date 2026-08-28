namespace Penuel.Application.Abstractions;

/// <summary>
/// Marca un caso de uso que solo puede ejecutar una cuenta con el rol <c>Pastor</c>.
/// </summary>
/// <remarks>
/// Implementa la regla 7.5 (AssignRole/RevokeRole son exclusivos del Pastor) y la regla por
/// defecto de la Sección 8.2 (todo endpoint requiere Pastor salvo Login, RefreshToken y
/// GetMyCapabilities). Lo verifica <c>AuthorizationBehavior</c> en el pipeline de MediatR,
/// de modo que la autorización se resuelve en Penuel.Application (Sección 5.4) y no depende
/// de que alguien se acuerde de poner el atributo en el controlador.
/// </remarks>
public interface IRequirePastor;
