using Content.Shared.IdentityManagement.Components;

namespace Content.Shared._Respiral.Identity;

/// <summary>
/// Поднимается на сущности при сборке её отображаемого имени, чтобы средневековые
/// грамоты могли переопределить <see cref="IdentityRepresentation.PresumedName"/> /
/// <see cref="IdentityRepresentation.PresumedJob"/>.
/// <para>
/// обрабатывает напрямую (паспорт в руке через <see cref="PresentingCredentialComponent"/>).
/// Релей на надетый герб.
/// </para>
/// </summary>
[ByRefEvent]
public readonly struct GetMedievalCredentialEvent
{
    public readonly IdentityRepresentation Representation;

    public GetMedievalCredentialEvent(IdentityRepresentation representation)
    {
        Representation = representation;
    }
}
