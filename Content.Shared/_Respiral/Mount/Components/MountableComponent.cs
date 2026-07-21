using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.Mount.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RiderComponent : Component
{
    /// <summary>
    /// The animal that this character is currently riding on
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Mount;
}
