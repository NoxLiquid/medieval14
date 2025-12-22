namespace Content.Shared.Blacksmithing.Components;

[RegisterComponent]
public sealed partial class BellowsComponent : Component
{
    [DataField("airFlow")]
    public float AirFlow = 12.5f;

    [DataField("condition")]
    public float Condition = 1.0f;
}
