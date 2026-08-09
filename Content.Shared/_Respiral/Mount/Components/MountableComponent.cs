using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.Mount.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MountableComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Rider;

    [DataField]
    public float MountedSpeed = 1.5f;

    [DataField]
    public float DefaultSpeed = 1.5f;

    [DataField]
    public bool ControlMovement = true;

    /// <summary>
    /// If true, the mount can only be ridden while it has an item in its "saddle" inventory slot
    /// </summary>
    [DataField]
    public bool NeedsSaddle;
}
