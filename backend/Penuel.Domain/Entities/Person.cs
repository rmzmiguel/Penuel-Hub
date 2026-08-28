using Penuel.Domain.Enums;

namespace Penuel.Domain.Entities;

/// <summary>
/// Cualquier ser humano que el sistema necesita recordar. Una fila aquí NO implica
/// absolutamente nada sobre su relación con la iglesia (Sección 3.1): puede ser un miembro
/// oficial de toda la vida, un visitante que alguien invitó la semana pasada, o el propio Pastor.
/// La membresía oficial vive en <see cref="Membership"/>; el acceso al sistema, en <see cref="UserAccount"/>.
/// </summary>
public sealed class Person
{
    private Person() { }

    public Guid Id { get; private set; }
    public Guid ChurchId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public DateOnly? DateOfBirth { get; private set; }
    public string? PhoneNumber { get; private set; }
    public PersonStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Auditoría de captura distribuida (regla 7.4): se guarda el PersonId, no el UserAccountId.</summary>
    public Guid? CreatedByPersonId { get; private set; }
    public Guid? UpdatedByPersonId { get; private set; }

    public static Person Register(
        Guid churchId,
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        string? phoneNumber,
        Guid? createdByPersonId,
        DateTimeOffset now)
    {
        return new Person
        {
            Id = Guid.NewGuid(),
            ChurchId = churchId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DateOfBirth = dateOfBirth,
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
            Status = PersonStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByPersonId = createdByPersonId,
            UpdatedByPersonId = createdByPersonId
        };
    }

    public void UpdateDetails(
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        string? phoneNumber,
        Guid? updatedByPersonId,
        DateTimeOffset now)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Stamp(updatedByPersonId, now);
    }

    /// <summary>Borrado lógico (regla 7.3). Nunca se elimina la fila.</summary>
    public void Deactivate(Guid? updatedByPersonId, DateTimeOffset now)
    {
        Status = PersonStatus.Inactive;
        Stamp(updatedByPersonId, now);
    }

    public void Reactivate(Guid? updatedByPersonId, DateTimeOffset now)
    {
        Status = PersonStatus.Active;
        Stamp(updatedByPersonId, now);
    }

    public void MarkAsDeceased(Guid? updatedByPersonId, DateTimeOffset now)
    {
        Status = PersonStatus.Deceased;
        Stamp(updatedByPersonId, now);
    }

    private void Stamp(Guid? updatedByPersonId, DateTimeOffset now)
    {
        UpdatedByPersonId = updatedByPersonId;
        UpdatedAt = now;
    }
}
