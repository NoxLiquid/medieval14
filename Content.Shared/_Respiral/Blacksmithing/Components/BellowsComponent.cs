namespace Content.Shared._Respiral.Blacksmithing.Components;

[RegisterComponent]
public sealed partial class BellowsComponent : Component
{
    [DataField]
    public float AirFlow = 12.5f;

    [DataField]
    public float Condition = 1.0f;
}
