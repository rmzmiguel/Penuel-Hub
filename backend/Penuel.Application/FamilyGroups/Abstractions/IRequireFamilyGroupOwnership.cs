using Penuel.Application.Abstractions;

namespace Penuel.Application.FamilyGroups.Abstractions;

/// <summary>
/// Acto operativo sobre un Grupo Familiar concreto: lo puede ejecutar el Pastor, o la persona
/// que es Anfitriona o Encargada de ESE grupo (Sección 8.2).
/// </summary>
/// <remarks>
/// Hereda de <see cref="IAuthorizeInHandler"/> y no de <c>IRequireAuthorization</c> porque no
/// hay ningún rol ni cargo que enumerar: quien opera un grupo puede no tener absolutamente
/// ningún permiso de sistema. El nombre existe igualmente para que leer la firma de un comando
/// diga de qué va su autorización sin abrir el handler.
///
/// Todo comando que lo declare DEBE resolver el permiso con
/// <c>FamilyGroupPermissions.LoadOwnedAsync</c> antes de escribir nada.
/// </remarks>
public interface IRequireFamilyGroupOwnership : IAuthorizeInHandler;
