using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.Hitch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HitchComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Horse;

    [DataField]
    public TimeSpan AttachDelay = TimeSpan.FromSeconds(2);
}
