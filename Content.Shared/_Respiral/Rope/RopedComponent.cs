using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.Rope;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RopedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Partner;

    [ViewVariables]
    public EntityUid? Rope;
}
