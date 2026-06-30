using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.Identity.Components;

/// <summary>
/// Транзиентное состояние «грамота развёрнута» на носителе — режим «предъявить всем».
/// Пока висит, паспорт <see cref="Passport"/> PresumedName носителя видят все вокруг.
/// Снимается при убирании грамоты или выпадении её из рук. Знание не запоминается —
/// для durable-узнавания одним читателем будет отдельный per-viewer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PresentingCredentialComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Passport;
}
