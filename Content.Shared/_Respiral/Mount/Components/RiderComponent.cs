using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.Mount.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RiderComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Mount;
}
