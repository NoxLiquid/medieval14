using Content.Shared._Respiral.Temperature.Systems;
using Content.Shared.Temperature.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.Temperature.Components;

[RegisterComponent, Access(typeof(SharedHeatSourceSystem))]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeatSourceComponent : Component
{
    [DataField]
    public float DegreesPerSecond = 1f;

    [DataField, AutoNetworkedField]
    public bool IsActive;
}
