namespace Penuel.Application.Abstractions;

/// <summary>
/// Marca un caso de uso cuya autorización NO puede decidirse antes de cargar el recurso.
/// El pipeline garantiza únicamente que hay una sesión válida; el permiso lo resuelve el
/// handler.
/// </summary>
/// <remarks>
/// Es el TERCER patrón de autorización del sistema, y hacía falta nombrarlo:
///
///   1. Por <c>Role</c>      — viaja en el JWT.            <see cref="IRequirePastor"/>
///   2. Por <c>Position</c>  — se resuelve contra la base. <see cref="IRequireAuthorization"/>
///   3. Por el RECURSO       — se compara a la persona autenticada contra un campo del
///      propio recurso (p. ej. <c>FamilyGroup.HostPersonId</c>), sin que exista ningún rol
///      ni cargo de por medio.
///
/// El tercero no se puede expresar con los dos primeros, y no es un detalle: en la rama de
/// Grupos Familiares, cualquier persona del directorio —sin un solo permiso de sistema— queda
/// autorizada a operar un grupo por el mero hecho de SER esa casa. Un
/// <see cref="IRequireAuthorization"/> con listas vacías la rechazaría antes de llegar al
/// handler, y dejar el caso de uso sin marcador lo abriría a cualquiera en silencio.
///
/// Este marcador no es una puerta abierta: es una PROMESA de que el handler decide, y el
/// guardián estructural de las pruebas lo acepta solo porque es explícito. Todo handler que
/// lo declare está obligado a comprobar el permiso contra el recurso antes de escribir nada.
/// </remarks>
public interface IAuthorizeInHandler;
