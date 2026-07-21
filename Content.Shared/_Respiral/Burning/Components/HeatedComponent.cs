namespace Content.Shared._Respiral.Burning.Components;

[RegisterComponent]
public sealed partial class HeatedComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float CurrentTemperature = 0.0f;

    [DataField("coolingRate")]
    public float CoolingRate = 7.5f;

    [DataField("overheatThreshold")]
    public float OverheatThreshold = 1200f;
}
