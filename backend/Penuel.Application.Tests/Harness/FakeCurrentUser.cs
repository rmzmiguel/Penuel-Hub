using Penuel.Application.Abstractions;

namespace Penuel.Application.Tests.Harness;

/// <summary>Doble de <see cref="ICurrentUser"/> que la prueba controla a voluntad.</summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; private set; }
    public Guid? UserAccountId { get; private set; }
    public Guid? PersonId { get; private set; }
    public string? Email { get; private set; }
    public IReadOnlyCollection<string> Roles { get; private set; } = [];

    public void SignInAs(Guid personId, Guid userAccountId, params string[] roles)
    {
        IsAuthenticated = true;
        PersonId = personId;
        UserAccountId = userAccountId;
        Email = "sesion@penuel.mx";
        Roles = roles;
    }

    public void SignOut()
    {
        IsAuthenticated = false;
        PersonId = null;
        UserAccountId = null;
        Email = null;
        Roles = [];
    }
}
